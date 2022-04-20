using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using Service.Liquidity.Hedger.Domain.Extensions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services
{
    public class HedgeService : IHedgeService
    {
        private readonly ILogger<HedgeService> _logger;
        private readonly IExternalMarket _externalMarket;
        private readonly IHedgeOperationsStorage _hedgeOperationsStorage;
        private readonly ICurrentPricesCache _currentPricesCache;
        private readonly IExchangesAnalyzer _exchangesAnalyzer;
        private readonly IHedgeSettingsStorage _hedgeSettingsStorage;

        public HedgeService(
            ILogger<HedgeService> logger,
            IExternalMarket externalMarket,
            IHedgeOperationsStorage hedgeOperationsStorage,
            ICurrentPricesCache currentPricesCache,
            IExchangesAnalyzer exchangesAnalyzer,
            IHedgeSettingsStorage hedgeSettingsStorage
        )
        {
            _logger = logger;
            _externalMarket = externalMarket;
            _hedgeOperationsStorage = hedgeOperationsStorage;
            _currentPricesCache = currentPricesCache;
            _exchangesAnalyzer = exchangesAnalyzer;
            _hedgeSettingsStorage = hedgeSettingsStorage;
        }

        public async Task<HedgeOperation> HedgeAsync(HedgeInstruction hedgeInstruction)
        {
            var hedgeOperation = new HedgeOperation
            {
                Id = Guid.NewGuid().ToString(),
                TargetVolume = hedgeInstruction.TargetVolume,
                HedgeTrades = new List<HedgeTrade>(),
                CreatedDate = DateTime.UtcNow,
                TargetAsset = hedgeInstruction.TargetAssetSymbol,
                TradedVolume = 0
            };
            var settings = await _hedgeSettingsStorage.GetAsync();

            foreach (var exchange in settings.EnabledExchanges ?? new List<string>())
            {
                var directMarkets = await _exchangesAnalyzer
                    .FindDirectMarketsAsync(exchange, hedgeInstruction);

                foreach (var market in directMarkets.OrderByDescending(m => m.Weight))
                {
                    if (hedgeOperation.IsFullyHedged())
                    {
                        break;
                    }

                    await HedgeOnDirectMarketAsync(hedgeInstruction, hedgeOperation, market,
                        settings.DirectMarketLimitTradeSteps ?? new List<LimitTradeStep>());
                }

                _logger.LogInformation(
                    "Traded on DirectMarkets: TargetVolume={@TargetVolume}, TradedVolume={@TradedVolume}",
                    hedgeInstruction.TargetVolume, hedgeOperation.TradedVolume);

                foreach (var transitAsset in settings.IndirectMarketTransitAssets ?? new List<string>())
                {
                    if (hedgeOperation.IsFullyHedged())
                    {
                        break;
                    }

                    await HedgeOnIndirectMarketsAsync(hedgeInstruction, transitAsset, hedgeOperation, exchange,
                        settings.IndirectMarketLimitTradeSteps ?? new List<LimitTradeStep>());
                    
                    _logger.LogInformation(
                        "Traded on IndirectMarkets: TargetVolume={@TargetVolume}, TradedVolume={@TradedVolume}",
                        hedgeInstruction.TargetVolume, hedgeOperation.TradedVolume);
                }

                _logger.LogInformation("HedgeOperation ended. {@Operation}", hedgeOperation);

                if (hedgeOperation.IsFullyHedged())
                {
                    break;
                }
            }

            if (hedgeOperation.HedgeTrades.Any())
            {
                await _hedgeOperationsStorage.AddOrUpdateLastAsync(hedgeOperation);
            }

            return hedgeOperation;
        }

        private async Task HedgeOnDirectMarketAsync(HedgeInstruction hedgeInstruction, HedgeOperation hedgeOperation,
            DirectHedgeExchangeMarket market, ICollection<LimitTradeStep> limitTradeSteps)
        {
            if (hedgeOperation.IsFullyHedged())
            {
                return;
            }

            var remainingVolumeToTrade = hedgeInstruction.TargetVolume - hedgeOperation.TradedVolume;
            var marketPrice = _currentPricesCache.Get(market.ExchangeName, market.Info.Market);
            var side = market.Info.GetOrderSide(hedgeInstruction.TargetAssetSymbol);
            var tradeVolume =
                GetTradeVolume(remainingVolumeToTrade, marketPrice.Price, market.Balance.Free, side);

            if (Convert.ToDouble(tradeVolume) < market.Info.MinVolume)
            {
                _logger.LogWarning(
                    "Can't trade on market {@Market}. VolumeToBuy {@VolumeToBuy} < MarketMinVolume {@MinVolume}",
                    market.Info.Market, tradeVolume, market.Info.MinVolume);
                return;
            }

            if (limitTradeSteps.Any())
            {
                var trades = await MakeLimitTradesAsync(tradeVolume, side, market.Info,
                    market.ExchangeName, hedgeOperation.Id, marketPrice.Price, limitTradeSteps);
                hedgeOperation.AddTrades(trades);
            }
            else
            {
                var trade = await MakeMarketTradeAsync(tradeVolume, side, market.Info, market.ExchangeName,
                    hedgeOperation.Id);
                hedgeOperation.AddTrade(trade);
            }
        }

        private async Task HedgeOnIndirectMarketsAsync(HedgeInstruction hedgeInstruction, string transitAsset,
            HedgeOperation hedgeOperation, string exchangeName, ICollection<LimitTradeStep> limitTradeSteps)
        {
            if (hedgeOperation.IsFullyHedged())
            {
                return;
            }

            var indirectMarkets = await _exchangesAnalyzer.FindIndirectMarketsAsync(exchangeName,
                transitAsset, hedgeInstruction.TargetAssetSymbol, hedgeInstruction.PairAssets);

            foreach (var market in indirectMarkets.OrderByDescending(m => m.Weight))
            {
                if (hedgeOperation.IsFullyHedged())
                {
                    break;
                }
                
                _logger.LogInformation("Trying to HedgeOnIndirectMarket: {@Market}",
                    $"{market.TransitMarketInfo.Market} -> {market.TargetMarketInfo.Market}");
                
                var balancesResp = await _externalMarket.GetBalancesAsync(new GetBalancesRequest
                {
                    ExchangeName = exchangeName
                });
                var transitAssetBalance =
                    balancesResp.Balances?.FirstOrDefault(b => b.Symbol == market.TransitAssetSymbol)?.Free ?? 0;
                var transitPairAssetBalance =
                    balancesResp.Balances?.FirstOrDefault(b => b.Symbol == market.TransitPairAssetSymbol)?.Free ?? 0;

                var remainingVolumeToTradeInTargetAsset =
                    hedgeInstruction.TargetVolume - hedgeOperation.TradedVolume;
                var targetAssetPrice = _currentPricesCache.Get(market.ExchangeName,
                    market.TargetMarketInfo.Market);
                var targetAssetSide = market.TargetMarketInfo.GetOrderSide(hedgeInstruction.TargetAssetSymbol);
                var remainingVolumeToTradeInTransit = targetAssetSide == OrderSide.Buy
                    ? remainingVolumeToTradeInTargetAsset * targetAssetPrice.Price
                    : remainingVolumeToTradeInTargetAsset / targetAssetPrice.Price;
                var neededVolumeInTransitAsset = remainingVolumeToTradeInTransit - transitAssetBalance;
                var transitAssetPrice = _currentPricesCache.Get(market.ExchangeName,
                    market.TransitMarketInfo.Market);
                var transitAssetSide = market.TransitMarketInfo.GetOrderSide(transitAsset);
                var transitTradeVolume = GetTradeVolume(neededVolumeInTransitAsset,
                    transitAssetPrice.Price, transitPairAssetBalance, transitAssetSide);

                if (neededVolumeInTransitAsset <= 0)
                {
                    _logger.LogInformation(
                        "No need to make transit trade. There is enough balance on transit asset {@TransitAsset} {@TransitAssetBalance}",
                        transitAsset, transitAssetBalance);

                    var tradeVolume = GetTradeVolume(remainingVolumeToTradeInTargetAsset,
                        targetAssetPrice.Price, transitAssetBalance, targetAssetSide);

                    if (Convert.ToDouble(tradeVolume) < market.TargetMarketInfo.MinVolume)
                    {
                        _logger.LogWarning(
                            "Can't on IndirectMarket {@TransitMarket} -> {@TargetMarket}. " +
                            "TargetTradeVolume be less than MarketMinVolume: {@TradeVolume} < {@MinVolume}",
                            market.TransitMarketInfo.Market, market.TargetMarketInfo.Market,
                            tradeVolume, market.TargetMarketInfo.MinVolume);
                        continue;
                    }
                    
                    if (limitTradeSteps?.Any() ?? false)
                    {
                        var targetTrades = await MakeLimitTradesAsync(tradeVolume, targetAssetSide,
                            market.TargetMarketInfo, market.ExchangeName, hedgeOperation.Id, targetAssetPrice.Price,
                            limitTradeSteps);
                        hedgeOperation.AddTrades(targetTrades);
                    }
                    else
                    {
                        var targetTrade = await MakeMarketTradeAsync(tradeVolume, targetAssetSide,
                            market.TargetMarketInfo, market.ExchangeName, hedgeOperation.Id);
                        hedgeOperation.AddTrade(targetTrade);
                    }
                }

                if (Convert.ToDouble(transitTradeVolume) < market.TransitMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on IndirectMarket {@TransitMarket} -> {@TargetMarket}. " +
                        "TransitTradeVolume is less than MarketMinVolume: {@TradeVolume} < {@MinVolume}",
                        market.TransitMarketInfo.Market, market.TargetMarketInfo.Market,
                        transitTradeVolume, market.TransitMarketInfo.MinVolume);
                    continue;
                }

                var volumeInTransitAssetAfterTransitTrade = transitAssetSide == OrderSide.Buy
                    ? transitTradeVolume.Truncate(market.TransitMarketInfo.VolumeAccuracy) /
                      transitAssetPrice.Price
                    : transitTradeVolume.Truncate(market.TransitMarketInfo.VolumeAccuracy) *
                      transitAssetPrice.Price;
                var availableVolumeInTransitAssetAfterTransitTrade =
                    volumeInTransitAssetAfterTransitTrade + transitAssetBalance;
                var targetTradeVolume = GetTradeVolume(remainingVolumeToTradeInTargetAsset,
                    targetAssetPrice.Price, availableVolumeInTransitAssetAfterTransitTrade,
                    targetAssetSide);

                if (Convert.ToDouble(targetTradeVolume) < market.TargetMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on IndirectMarket {@TransitMarket} -> {@TargetMarket}. " +
                        "TargetTradeVolume after TransitTrade will be less than MarketMinVolume: {@TradeVolume} < {@MinVolume}",
                        market.TransitMarketInfo.Market, market.TargetMarketInfo.Market,
                        targetTradeVolume, market.TargetMarketInfo.MinVolume);
                    continue;
                }

                if (limitTradeSteps?.Any() ?? false)
                {
                    var transitTrades = await MakeLimitTradesAsync(transitTradeVolume, transitAssetSide,
                        market.TransitMarketInfo, market.ExchangeName, hedgeOperation.Id, transitAssetPrice.Price,
                        limitTradeSteps);
                    hedgeOperation.AddTrades(transitTrades);

                    availableVolumeInTransitAssetAfterTransitTrade =
                        transitTrades.Sum(t => t.GetTradedVolume()) + transitAssetBalance;
                    targetTradeVolume = GetTradeVolume(remainingVolumeToTradeInTargetAsset,
                        targetAssetPrice.Price, availableVolumeInTransitAssetAfterTransitTrade,
                        targetAssetSide);
                    var targetTrades = await MakeLimitTradesAsync(targetTradeVolume, targetAssetSide,
                        market.TargetMarketInfo, market.ExchangeName, hedgeOperation.Id, targetAssetPrice.Price,
                        limitTradeSteps);
                    hedgeOperation.AddTrades(targetTrades);
                }
                else
                {
                    var transitTrade = await MakeMarketTradeAsync(transitTradeVolume, transitAssetSide,
                        market.TransitMarketInfo, market.ExchangeName, hedgeOperation.Id);
                    hedgeOperation.AddTrade(transitTrade);

                    var targetTrade = await MakeMarketTradeAsync(targetTradeVolume, targetAssetSide,
                        market.TargetMarketInfo, market.ExchangeName, hedgeOperation.Id);
                    hedgeOperation.AddTrade(targetTrade);
                }
                
                _logger.LogInformation("Made HedgeOnIndirectMarket {@Market}, TradedVolume={@TradedVolume}", 
                    $"{market.TransitMarketInfo.Market} -> {market.TargetMarketInfo.Market}", hedgeOperation.TradedVolume);
            }
        }

        private decimal GetTradeVolume(decimal targetVolume, decimal price, decimal balance, OrderSide side,
            decimal balancePercentToTrade = 1, int? volumeAccuracy = null)
        {
            var availableVolumeOnBalance = balance * balancePercentToTrade;
            var tradeVolume = 0m;

            if (side == OrderSide.Buy)
            {
                var possibleVolumeToBuy = availableVolumeOnBalance / price;
                tradeVolume = possibleVolumeToBuy < targetVolume
                    ? possibleVolumeToBuy // trade max possible volume
                    : targetVolume;
            }

            if (side == OrderSide.Sell)
            {
                var neededVolumeToSell = targetVolume / price;
                tradeVolume = availableVolumeOnBalance < neededVolumeToSell
                    ? availableVolumeOnBalance // trade max possible volume
                    : neededVolumeToSell;
            }

            return volumeAccuracy == null ? tradeVolume : targetVolume.Truncate(volumeAccuracy.Value);
        }

        private async Task<HedgeTrade> MakeMarketTradeAsync(decimal tradeVolume, OrderSide orderSide,
            ExchangeMarketInfo marketInfo, string exchangeName, string operationId)
        {
            var request = new MarketTradeRequest
            {
                Side = orderSide,
                Market = marketInfo.Market,
                Volume = Convert.ToDouble(tradeVolume),
                ExchangeName = exchangeName,
                OppositeVolume = 0,
                ReferenceId = Guid.NewGuid().ToString(),
            };

            var response = await _externalMarket.MarketTrade(request);

            var hedgeTrade = new HedgeTrade
            {
                BaseAsset = marketInfo.BaseAsset,
                BaseVolume = Math.Abs(Convert.ToDecimal(response.Volume)),
                ExchangeName = request.ExchangeName,
                HedgeOperationId = operationId,
                QuoteAsset = marketInfo.QuoteAsset,
                QuoteVolume = Math.Abs(Convert.ToDecimal(response.Price * response.Volume)),
                Price = Convert.ToDecimal(response.Price),
                Id = response.ReferenceId ?? request.ReferenceId,
                CreatedDate = DateTime.UtcNow,
                ExternalId = response.Id,
                FeeAsset = response.FeeSymbol,
                FeeVolume = Math.Abs(Convert.ToDecimal(response.FeeVolume)),
                Market = response.Market,
                Side = response.Side,
                Type = OrderType.Market
            };

            _logger.LogInformation("Made MarketTrade. Request: {@Request} Response: {@Response}", request, response);

            return hedgeTrade;
        }

        private async Task<ICollection<HedgeTrade>> MakeLimitTradesAsync(decimal tradeVolume, OrderSide orderSide,
            ExchangeMarketInfo marketInfo, string exchangeName, string operationId, decimal price,
            IEnumerable<LimitTradeStep> steps)
        {
            var trades = new List<HedgeTrade>();
            var tradedVolume = 0m;
            
            _logger.LogInformation("Trying to MakeLimitTades {@Exchange} {@Market} {@Volume} {@OrderSide}",
                exchangeName, marketInfo.Market, tradeVolume, orderSide);

            foreach (var step in steps.OrderBy(s => s.Number))
            {
                var currentPrice = _currentPricesCache.Get(exchangeName, marketInfo.Market);

                var request = new MakeLimitTradeRequest
                {
                    Side = orderSide,
                    Market = marketInfo.Market,
                    Volume = tradeVolume - tradedVolume,
                    ExchangeName = exchangeName,
                    ReferenceId = Guid.NewGuid().ToString(),
                    PriceLimit = step.CalculatePriceLimit(orderSide, price, currentPrice.Price),
                    TimeLimit = step.DurationLimit
                };

                _logger.LogInformation(
                    "Calculated PriceLimit: {@PriceLimit}; InitialPrice: {@InitialPrice}; CurrentPrice: {@CurrentPrice}; Step: {@Step}",
                    request.PriceLimit, price, currentPrice.Price, step);

                if (request.Volume < Convert.ToDecimal(marketInfo.MinVolume))
                {
                    _logger.LogInformation(
                        "Can't make LimitTrade. TradeVolume: {@Volume} < MarketMinVolume: {@MinVolume}",
                        request.Volume, marketInfo.MinVolume);
                    break;
                }

                var response = await _externalMarket.MakeLimitTradeAsync(request);

                var hedgeTrade = new HedgeTrade
                {
                    BaseAsset = marketInfo.BaseAsset,
                    BaseVolume = Math.Abs(Convert.ToDecimal(response.Volume)),
                    ExchangeName = request.ExchangeName,
                    HedgeOperationId = operationId,
                    QuoteAsset = marketInfo.QuoteAsset,
                    QuoteVolume = Math.Abs(Convert.ToDecimal(response.Price * response.Volume)),
                    Price = Convert.ToDecimal(response.Price > 0 ? response.Price : request.PriceLimit),
                    Id = response.ReferenceId ?? request.ReferenceId,
                    CreatedDate = DateTime.UtcNow,
                    ExternalId = response.Id,
                    FeeAsset = response.FeeSymbol,
                    FeeVolume = Math.Abs(Convert.ToDecimal(response.FeeVolume)),
                    Market = response.Market,
                    Side = response.Side,
                    Type = OrderType.Limit
                };

                _logger.LogInformation("Made LimitTrade. Step: {@Step}; Request: {@Request}; Response: {@Response}",
                    step, request, response);

                trades.Add(hedgeTrade);
                tradedVolume += hedgeTrade.GetTradedVolume();

                if (tradedVolume >= tradeVolume.Truncate(marketInfo.VolumeAccuracy))
                {
                    break;
                }
            }
            
            _logger.LogInformation("MakeLimitTrades ended. Market: {@Market} TradedVolume: {@TradedVolume}",
                marketInfo.Market, tradedVolume);

            return trades;
        }
    }
}