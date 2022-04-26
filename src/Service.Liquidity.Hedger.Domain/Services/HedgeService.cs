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
        private readonly IExchangesAnalyzer _exchangesAnalyzer;
        private readonly IHedgeSettingsStorage _hedgeSettingsStorage;
        private readonly IPricesService _pricesService;

        public HedgeService(
            ILogger<HedgeService> logger,
            IExternalMarket externalMarket,
            IHedgeOperationsStorage hedgeOperationsStorage,
            IExchangesAnalyzer exchangesAnalyzer,
            IHedgeSettingsStorage hedgeSettingsStorage,
            IPricesService pricesService
        )
        {
            _logger = logger;
            _externalMarket = externalMarket;
            _hedgeOperationsStorage = hedgeOperationsStorage;
            _exchangesAnalyzer = exchangesAnalyzer;
            _hedgeSettingsStorage = hedgeSettingsStorage;
            _pricesService = pricesService;
        }

        public async Task<HedgeOperation> HedgeAsync(HedgeInstruction hedgeInstruction)
        {
            var operation = new HedgeOperation
            {
                Id = Guid.NewGuid().ToString(),
                TargetVolume = hedgeInstruction.TargetVolume,
                HedgeTrades = new List<HedgeTrade>(),
                CreatedDate = DateTime.UtcNow,
                TargetAsset = hedgeInstruction.TargetAssetSymbol,
                TradedVolume = 0,
                TargetSide = hedgeInstruction.TargetSide
            };

            try
            {
                var settings = await _hedgeSettingsStorage.GetAsync();

                foreach (var exchange in settings.EnabledExchanges)
                {
                    await HedgeOnDirectMarketsAsync(hedgeInstruction, operation, exchange);
                    
                    if (operation.IsFullyHedged())
                    {
                        break;
                    }
                    
                    await HedgeOnIndirectMarketsAsync(hedgeInstruction, operation, exchange);

                    if (operation.IsFullyHedged())
                    {
                        break;
                    }
                }
                
                _logger.LogInformation("Hedge ended. {@Operation}", operation);

                if (operation.HedgeTrades.Any())
                {
                    await _hedgeOperationsStorage.AddOrUpdateLastAsync(operation);
                }

                return operation;
            }
            catch (Exception ex)
            {
                if (operation.HedgeTrades.Any())
                {
                    _logger.LogWarning(ex, "Failed to fully hedge. {@Instruction} {@Operation}",
                        hedgeInstruction, operation);
                    return operation;
                }

                throw;
            }
        }

        private async Task HedgeOnDirectMarketsAsync(HedgeInstruction hedgeInstruction, HedgeOperation hedgeOperation,
            string exchange)
        {
            var directMarkets = await _exchangesAnalyzer
                .FindDirectMarketsAsync(exchange, hedgeInstruction);

            foreach (var market in directMarkets.OrderByDescending(m => m.Weight))
            {
                if (hedgeOperation.IsFullyHedged())
                {
                    break;
                }

                await HedgeOnDirectMarketAsync(hedgeInstruction, hedgeOperation, market);
            }

            _logger.LogInformation(
                "Hedged on DirectMarkets: TargetVolume={@TargetVolume}, TradedVolume={@TradedVolume}",
                hedgeInstruction.TargetVolume, hedgeOperation.TradedVolume);
        }

        private async Task HedgeOnDirectMarketAsync(HedgeInstruction hedgeInstruction, HedgeOperation hedgeOperation,
            DirectHedgeExchangeMarket market)
        {
            if (hedgeOperation.IsFullyHedged())
            {
                return;
            }

            var remainingVolumeToTrade = hedgeInstruction.TargetVolume - hedgeOperation.TradedVolume;
            var price = await _pricesService.GetConvertPriceAsync(market.ExchangeName, market.Info.Market);
            var tradeVolume = 0m;
            var side = OrderSide.UnknownOrderSide;

            if (hedgeInstruction.TargetSide == OrderSide.Buy)
            {
                side = market.Info.GetOrderSideToBuyAsset(hedgeInstruction.TargetAssetSymbol);
                tradeVolume = GetTradeVolume(remainingVolumeToTrade, price, market.AvailablePairAssetVolume, side);
            }
            else if (hedgeInstruction.TargetSide == OrderSide.Sell)
            {
                var tradeVolumeInTargetAsset = Math.Min(remainingVolumeToTrade,
                    market.AvailableTargetAssetVolume - hedgeOperation.TradedVolume);

                if (hedgeInstruction.TargetAssetSymbol == market.Info.BaseAsset)
                {
                    tradeVolume = tradeVolumeInTargetAsset;
                    side = OrderSide.Sell;
                }
                else
                {
                    tradeVolume = tradeVolumeInTargetAsset / price;
                    side = OrderSide.Buy;
                }
            }

            if (Convert.ToDouble(tradeVolume) < market.Info.MinVolume)
            {
                _logger.LogWarning(
                    "Can't trade on market {@Market}. VolumeToBuy {@VolumeToBuy} < MarketMinVolume {@MinVolume}",
                    market.Info.Market, tradeVolume, market.Info.MinVolume);
                return;
            }

            var settings = await _hedgeSettingsStorage.GetAsync();

            if (settings.DirectMarketLimitTradeSteps.Any())
            {
                var trades = await MakeLimitTradesAsync(tradeVolume, side, market.Info,
                    market.ExchangeName, hedgeOperation.Id, settings.DirectMarketLimitTradeSteps);
                hedgeOperation.AddTrades(trades);
            }
            else
            {
                var trade = await MakeMarketTradeAsync(tradeVolume, side, market.Info, market.ExchangeName,
                    hedgeOperation.Id);
                hedgeOperation.AddTrade(trade);
            }
        }

        private async Task HedgeOnIndirectMarketsAsync(HedgeInstruction hedgeInstruction,
            HedgeOperation hedgeOperation, string exchangeName)
        {
            var settings = await _hedgeSettingsStorage.GetAsync();

            foreach (var transitAsset in settings.IndirectMarketTransitAssets)
            {
                if (hedgeOperation.IsFullyHedged())
                {
                    return;
                }

                if (hedgeInstruction.TargetSide == OrderSide.Sell)
                {
                    await SellOnIndirectMarketsAsync(hedgeInstruction, transitAsset, hedgeOperation, exchangeName,
                        settings.IndirectMarketLimitTradeSteps);
                }
                else if (hedgeInstruction.TargetSide == OrderSide.Buy)
                {
                    await BuyOnIndirectMarketsAsync(hedgeInstruction, transitAsset, hedgeOperation, exchangeName,
                        settings.IndirectMarketLimitTradeSteps);
                }
            }

            _logger.LogInformation(
                "Hedged on IndirectMarkets: TargetVolume={@TargetVolume}, TradedVolume={@TradedVolume}",
                hedgeInstruction.TargetVolume, hedgeOperation.TradedVolume);
        }

        private async Task BuyOnIndirectMarketsAsync(HedgeInstruction hedgeInstruction, string transitAsset,
            HedgeOperation hedgeOperation, string exchangeName, ICollection<LimitTradeStep> limitTradeSteps)
        {
            var indirectMarkets = await _exchangesAnalyzer.FindIndirectMarketsToBuyAssetAsync(exchangeName,
                transitAsset, hedgeInstruction.TargetAssetSymbol, hedgeInstruction.PairAssets);
            var freeVolumeInTransitAssetAfterTransitTrades = 0m;

            foreach (var market in indirectMarkets.OrderByDescending(m => m.Weight))
            {
                if (hedgeOperation.IsFullyHedged())
                {
                    break;
                }

                _logger.LogInformation("Trying to HedgeOnIndirectMarket: {@Market}", market.GetMarketsDesc());

                var remainingVolumeToTradeInTargetAsset =
                    hedgeInstruction.TargetVolume - hedgeOperation.TradedVolume;
                var targetAssetPrice =
                    await _pricesService.GetConvertPriceAsync(market.ExchangeName, market.SecondTradeMarketInfo.Market);
                var targetAssetSide =
                    market.SecondTradeMarketInfo.GetOrderSideToBuyAsset(hedgeInstruction.TargetAssetSymbol);
                var remainingVolumeToTradeInTransitAsset = targetAssetSide == OrderSide.Buy
                    ? remainingVolumeToTradeInTargetAsset * targetAssetPrice
                    : remainingVolumeToTradeInTargetAsset / targetAssetPrice;

                if (remainingVolumeToTradeInTransitAsset <= freeVolumeInTransitAssetAfterTransitTrades)
                {
                    _logger.LogInformation(
                        "No need to make transit trade on {@Market}. There is enough free volume after prev transit trades {@TransitAsset} {@TransitAssetBalance}",
                        market.GetMarketsDesc(), transitAsset, freeVolumeInTransitAssetAfterTransitTrades);

                    var targetTradeVolume = GetTradeVolume(remainingVolumeToTradeInTargetAsset,
                        targetAssetPrice, freeVolumeInTransitAssetAfterTransitTrades, targetAssetSide);

                    if (Convert.ToDouble(targetTradeVolume) < market.SecondTradeMarketInfo.MinVolume)
                    {
                        _logger.LogWarning(
                            "Can't on IndirectMarket {@Market}.TargetTradeVolume be less than MarketMinVolume: {@TradeVolume} < {@MinVolume}",
                            market.GetMarketsDesc(), targetTradeVolume, market.SecondTradeMarketInfo.MinVolume);
                        continue;
                    }

                    if (limitTradeSteps?.Any() ?? false)
                    {
                        var targetTrades = await MakeLimitTradesAsync(targetTradeVolume, targetAssetSide,
                            market.SecondTradeMarketInfo, market.ExchangeName, hedgeOperation.Id, limitTradeSteps);
                        hedgeOperation.AddTrades(targetTrades);
                    }
                    else
                    {
                        var targetTrade = await MakeMarketTradeAsync(targetTradeVolume, targetAssetSide,
                            market.SecondTradeMarketInfo, market.ExchangeName, hedgeOperation.Id);
                        hedgeOperation.AddTrade(targetTrade);
                    }

                    _logger.LogInformation(
                        "Made HedgeOnIndirectMarket with skipping transit trade {@Market}, TradedVolume={@TradedVolume}",
                        market.GetMarketsDesc(), hedgeOperation.TradedVolume);

                    continue;
                }

                var neededVolumeInTransitAsset =
                    remainingVolumeToTradeInTransitAsset - freeVolumeInTransitAssetAfterTransitTrades;
                var transitAssetPrice =
                    await _pricesService.GetConvertPriceAsync(market.ExchangeName, market.FirstTradeMarketInfo.Market);
                var transitAssetSide = market.FirstTradeMarketInfo.GetOrderSideToBuyAsset(transitAsset);
                var transitTradeVolume = GetTradeVolume(neededVolumeInTransitAsset,
                    transitAssetPrice, market.PairAssetAvailableVolume, transitAssetSide);

                if (Convert.ToDouble(transitTradeVolume) < market.FirstTradeMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on IndirectMarket {@Market}. TransitTradeVolume is less than MarketMinVolume: {@TradeVolume} < {@MinVolume}",
                        market.GetMarketsDesc(), transitTradeVolume, market.FirstTradeMarketInfo.MinVolume);
                    continue;
                }

                var volumeInTransitAssetAfterTransitTradeGuess = transitAssetSide == OrderSide.Buy
                    ? transitTradeVolume.Truncate(market.FirstTradeMarketInfo.VolumeAccuracy) /
                      transitAssetPrice
                    : transitTradeVolume.Truncate(market.FirstTradeMarketInfo.VolumeAccuracy) *
                      transitAssetPrice;
                var targetTradeVolumeGuess = GetTradeVolume(remainingVolumeToTradeInTargetAsset,
                    targetAssetPrice, volumeInTransitAssetAfterTransitTradeGuess, targetAssetSide);

                if (Convert.ToDouble(targetTradeVolumeGuess) < market.SecondTradeMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on IndirectMarket {@Market}. TargetTradeVolume after TransitTrade will be less than MarketMinVolume: {@TradeVolume} < {@MinVolume}",
                        market.GetMarketsDesc(), targetTradeVolumeGuess, market.SecondTradeMarketInfo.MinVolume);
                    continue;
                }

                if (limitTradeSteps?.Any() ?? false)
                {
                    var transitTrades = await MakeLimitTradesAsync(transitTradeVolume, transitAssetSide,
                        market.FirstTradeMarketInfo, market.ExchangeName, hedgeOperation.Id, limitTradeSteps);
                    hedgeOperation.AddTrades(transitTrades);

                    var volumeInTransitAssetAfterTransitTrade = transitTrades.Sum(t => t.GetTradedVolume(transitAsset));
                    freeVolumeInTransitAssetAfterTransitTrades += volumeInTransitAssetAfterTransitTrade;
                    var targetTradeVolume = GetTradeVolume(remainingVolumeToTradeInTargetAsset,
                        targetAssetPrice, freeVolumeInTransitAssetAfterTransitTrades, targetAssetSide,
                        market.SecondTradeMarketInfo.VolumeAccuracy);
                    var targetTrades = await MakeLimitTradesAsync(targetTradeVolume, targetAssetSide,
                        market.SecondTradeMarketInfo, market.ExchangeName, hedgeOperation.Id, limitTradeSteps);
                    hedgeOperation.AddTrades(targetTrades);
                    freeVolumeInTransitAssetAfterTransitTrades -=
                        targetTrades.Sum(t => t.GetTradedVolume(transitAsset));

                    var actualTargetTradeVolume =
                        targetTrades.Sum(t => t.GetTradedVolume(hedgeInstruction.TargetAssetSymbol));
                    var bigChangesOnMarket = actualTargetTradeVolume < targetTradeVolume;

                    if (bigChangesOnMarket)
                    {
                        _logger.LogWarning(
                            "Break from IndirectMarket {@Market}. TargetTradeVolume isn't filled after trade: {@ActualTargetTradeVolume} < {@TargetTradeVolume}",
                            market.GetMarketsDesc(), actualTargetTradeVolume, targetTradeVolume);
                        break;
                    }
                }
                else
                {
                    var transitTrade = await MakeMarketTradeAsync(transitTradeVolume, transitAssetSide,
                        market.FirstTradeMarketInfo, market.ExchangeName, hedgeOperation.Id);
                    hedgeOperation.AddTrade(transitTrade);

                    var volumeInTransitAssetAfterTransitTrade = transitTrade.GetTradedVolume(transitAsset);
                    freeVolumeInTransitAssetAfterTransitTrades += volumeInTransitAssetAfterTransitTrade;
                    var targetTradeVolume = GetTradeVolume(remainingVolumeToTradeInTargetAsset,
                        targetAssetPrice, freeVolumeInTransitAssetAfterTransitTrades, targetAssetSide);
                    var targetTrade = await MakeMarketTradeAsync(targetTradeVolume, targetAssetSide,
                        market.SecondTradeMarketInfo, market.ExchangeName, hedgeOperation.Id);
                    hedgeOperation.AddTrade(targetTrade);
                    freeVolumeInTransitAssetAfterTransitTrades -= targetTrade.GetTradedVolume(transitAsset);
                }

                _logger.LogInformation("Made BuyOnIndirectMarket {@Market}, TradedVolume={@TradedVolume}",
                    market.GetMarketsDesc(), hedgeOperation.TradedVolume);
            }
        }

        private async Task SellOnIndirectMarketsAsync(HedgeInstruction hedgeInstruction, string transitAsset,
            HedgeOperation hedgeOperation, string exchangeName, ICollection<LimitTradeStep> limitTradeSteps)
        {
            if (hedgeInstruction.TargetSide != OrderSide.Sell)
            {
                _logger.LogWarning("Can't SellOnIndirectMarkets. Only instructions with Sell side are possible");
                return;
            }

            var sellMarkets = await _exchangesAnalyzer.FindIndirectMarketsToSellAssetAsync(exchangeName,
                transitAsset, hedgeInstruction);

            foreach (var market in sellMarkets.OrderByDescending(m => m.Weight))
            {
                if (hedgeOperation.IsFullyHedged())
                {
                    break;
                }

                _logger.LogInformation("Trying to HedgeOnIndirectMarket: {@Market}", market.GetMarketsDesc());

                var remainingVolumeToTrade = hedgeInstruction.TargetVolume - hedgeOperation.TradedVolume;
                var firstTradePrice =
                    await _pricesService.GetConvertPriceAsync(exchangeName, market.FirstTradeMarketInfo.Market);
                var firstTradeSide =
                    market.FirstTradeMarketInfo.GetOrderSideToSellAsset(hedgeInstruction.TargetAssetSymbol);
                var tradeVolumeInTargetAsset = Math.Min(remainingVolumeToTrade,
                    market.TargetAssetAvailableVolume - hedgeOperation.TradedVolume);
                var firstTradeVolume = hedgeInstruction.TargetAssetSymbol == market.FirstTradeMarketInfo.BaseAsset
                    ? tradeVolumeInTargetAsset
                    : tradeVolumeInTargetAsset / firstTradePrice;

                if (Convert.ToDouble(firstTradeVolume) < market.FirstTradeMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on IndirectMarket {@Market}. TransitTradeVolume is less than MarketMinVolume: {@TradeVolume} < {@MinVolume}",
                        market.GetMarketsDesc(), firstTradeVolume, market.FirstTradeMarketInfo.MinVolume);
                    continue;
                }

                decimal volumeInTransitAssetAfterFirstTrade;

                if (limitTradeSteps?.Any() ?? false)
                {
                    var firstTrades = await MakeLimitTradesAsync(firstTradeVolume, firstTradeSide,
                        market.FirstTradeMarketInfo, market.ExchangeName, hedgeOperation.Id, limitTradeSteps);
                    hedgeOperation.AddTrades(firstTrades);
                    volumeInTransitAssetAfterFirstTrade = firstTrades.Sum(t => t.GetTradedVolume(transitAsset));
                }
                else
                {
                    var firstTrade = await MakeMarketTradeAsync(firstTradeVolume, firstTradeSide,
                        market.FirstTradeMarketInfo, market.ExchangeName, hedgeOperation.Id);
                    hedgeOperation.AddTrade(firstTrade);
                    volumeInTransitAssetAfterFirstTrade = firstTrade.GetTradedVolume(transitAsset);
                }

                var secondTradePrice =
                    await _pricesService.GetConvertPriceAsync(exchangeName, market.FirstTradeMarketInfo.Market);
                var secondTradeSide = market.SecondTradeMarketInfo.GetOrderSideToBuyAsset(market.PairAssetSymbol);
                var secondTradeVolume = transitAsset == market.SecondTradeMarketInfo.BaseAsset
                    ? volumeInTransitAssetAfterFirstTrade
                    : volumeInTransitAssetAfterFirstTrade / secondTradePrice;

                if (Convert.ToDouble(secondTradeVolume) < market.SecondTradeMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't make second trade on IndirectMarket {@Market}. TradeVolume  MarketMinVolume: {@TradeVolume} < {@MinVolume}",
                        market.GetMarketsDesc(), secondTradeVolume, market.SecondTradeMarketInfo.MinVolume);
                    continue;
                }

                if (limitTradeSteps?.Any() ?? false)
                {
                    var secondTrades = await MakeLimitTradesAsync(secondTradeVolume, secondTradeSide,
                        market.FirstTradeMarketInfo, market.ExchangeName, hedgeOperation.Id, limitTradeSteps);
                    hedgeOperation.AddTrades(secondTrades);
                }
                else
                {
                    var secondTrade = await MakeMarketTradeAsync(secondTradeVolume, secondTradeSide,
                        market.SecondTradeMarketInfo, market.ExchangeName, hedgeOperation.Id);
                    hedgeOperation.AddTrade(secondTrade);
                }

                _logger.LogInformation("Made SellOnIndirectMarket {@Market}, TradedVolume={@TradedVolume}",
                    market.GetMarketsDesc(), hedgeOperation.TradedVolume);
            }
        }

        private decimal GetTradeVolume(decimal targetAssetVolume, decimal price, decimal availablePairAssetVolume,
            OrderSide side, int? volumeAccuracy = null)
        {
            var availableVolumeOnBalance = availablePairAssetVolume;
            var tradeVolume = 0m;

            if (side == OrderSide.Buy)
            {
                var possibleVolumeToBuy = availableVolumeOnBalance / price;
                tradeVolume = possibleVolumeToBuy < targetAssetVolume
                    ? possibleVolumeToBuy // trade max possible volume
                    : targetAssetVolume;
            }

            if (side == OrderSide.Sell)
            {
                var neededVolumeToSell = targetAssetVolume / price;
                tradeVolume = availableVolumeOnBalance < neededVolumeToSell
                    ? availableVolumeOnBalance // trade max possible volume
                    : neededVolumeToSell;
            }

            return volumeAccuracy == null ? tradeVolume : tradeVolume.Truncate(volumeAccuracy.Value);
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
            ExchangeMarketInfo marketInfo, string exchangeName, string operationId, IEnumerable<LimitTradeStep> steps)
        {
            var trades = new List<HedgeTrade>();
            var tradedVolume = 0m;

            _logger.LogInformation("Trying to MakeLimitTades {@Exchange} {@Market} {@Volume} {@OrderSide}",
                exchangeName, marketInfo.Market, tradeVolume, orderSide);
            var initialPrice = await _pricesService.GetLimitTradePriceAsync(exchangeName, marketInfo.Market, orderSide);

            foreach (var step in steps.OrderBy(s => s.Number))
            {
                try
                {
                    var currentPrice =
                        await _pricesService.GetLimitTradePriceAsync(exchangeName, marketInfo.Market, orderSide);

                    var request = new MakeLimitTradeRequest
                    {
                        Side = orderSide,
                        Market = marketInfo.Market,
                        Volume = tradeVolume - tradedVolume,
                        ExchangeName = exchangeName,
                        ReferenceId = Guid.NewGuid().ToString(),
                        PriceLimit = step.CalculatePriceLimit(orderSide, initialPrice, currentPrice),
                        TimeLimit = step.DurationLimit
                    };

                    _logger.LogInformation(
                        "Calculated PriceLimit for {@Market}: {@PriceLimit}; InitialPrice: {@InitialPrice}; CurrentPrice: {@CurrentPrice}; Step: {@Step}",
                        marketInfo.Market, request.PriceLimit, initialPrice, currentPrice, step);

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

                    _logger.LogInformation(
                        "Made LimitTrade. Step: {@Step}; Request: {@Request}; Response: {@Response}; Trade: {@Trade}",
                        step, request, response, hedgeTrade);

                    trades.Add(hedgeTrade);
                    tradedVolume += hedgeTrade.BaseVolume;

                    if (tradedVolume >= tradeVolume.Truncate(marketInfo.VolumeAccuracy))
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (trades.Any())
                    {
                        _logger.LogWarning(ex, "Failed limit trade step {@Step}", step);
                        break;
                    }

                    throw;
                }
            }

            _logger.LogInformation("MakeLimitTrades ended. Market: {@Market} TradedVolume: {@TradedVolume}",
                marketInfo.Market, tradedVolume);

            return trades;
        }
    }
}