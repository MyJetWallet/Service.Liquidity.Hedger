﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
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

            if (!settings.EnabledExchanges?.Any() ?? true)
            {
                _logger.LogWarning("Can't Hedge. No enabled exchanges");
                return hedgeOperation;
            }

            foreach (var exchange in settings.EnabledExchanges)
            {
                var possibleMarkets = await _exchangesAnalyzer.FindDirectMarketsAsync(exchange, hedgeInstruction);

                foreach (var market in possibleMarkets)
                {
                    if (hedgeOperation.IsFullyHedged())
                    {
                        break;
                    }

                    var remainingVolumeToTrade = hedgeInstruction.TargetVolume - hedgeOperation.TradedVolume;
                    var marketPrice = _currentPricesCache.Get(market.ExchangeName, market.Info.Market);
                    var side = GetOrderSide(market.Info, hedgeInstruction.TargetAssetSymbol);
                    var tradeVolume =
                        GetTradeVolume(remainingVolumeToTrade, marketPrice.Price, market.Balance.Free, side);

                    if (Convert.ToDouble(tradeVolume) < market.Info.MinVolume)
                    {
                        _logger.LogWarning(
                            "Can't trade on market {@market}. VolumeToBuy {@volumeToBuy} < MarketMinVolume {@minVolume}",
                            market.Info.Market, tradeVolume, market.Info.MinVolume);
                        continue;
                    }

                    var trade = await MakeMarketTradeAsync(tradeVolume, side, market.Info, market.ExchangeName,
                        hedgeOperation.Id);
                    hedgeOperation.AddTrade(trade);
                }

                _logger.LogInformation(
                    "Traded on DirectMarkets: TargetVolume={@targetVolume}, TradedVolume={@tradedVolume}",
                    hedgeInstruction.TargetVolume, hedgeOperation.TradedVolume);

                foreach (var transitAsset in settings.IndirectMarketTransitAssets ?? new List<string>())
                {
                    if (hedgeOperation.IsFullyHedged())
                    {
                        break;
                    }

                    await HedgeOnIndirectMarketsAsync(hedgeInstruction, transitAsset, hedgeOperation, exchange);
                }

                _logger.LogInformation("HedgeOperation ended. {@operation}", hedgeOperation);

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

        private async Task HedgeOnIndirectMarketsAsync(HedgeInstruction hedgeInstruction, string transitAsset,
            HedgeOperation hedgeOperation, string exchangeName)
        {
            if (hedgeOperation.IsFullyHedged())
            {
                return;
            }

            var indirectMarkets = await _exchangesAnalyzer.FindIndirectMarketsAsync(exchangeName,
                transitAsset, hedgeInstruction.TargetAssetSymbol, hedgeInstruction.PairAssets);

            foreach (var market in indirectMarkets)
            {
                if (hedgeOperation.IsFullyHedged())
                {
                    break;
                }

                var remainingVolumeToTradeInTargetAsset =
                    hedgeInstruction.TargetVolume - hedgeOperation.TradedVolume;
                var targetAssetMarketPrice = _currentPricesCache.Get(market.ExchangeName,
                    market.TargetMarketInfo.Market);
                var targetAssetSide = GetOrderSide(market.TargetMarketInfo, hedgeInstruction.TargetAssetSymbol);
                var neededVolumeInTransitAsset = targetAssetSide == OrderSide.Buy
                    ? remainingVolumeToTradeInTargetAsset * targetAssetMarketPrice.Price
                    : remainingVolumeToTradeInTargetAsset / targetAssetMarketPrice.Price;
                var transitAssetMarketPrice = _currentPricesCache.Get(market.ExchangeName,
                    market.TransitMarketInfo.Market);
                var transitAssetSide = GetOrderSide(market.TransitMarketInfo, transitAsset);
                var transitTradeVolume = GetTradeVolume(neededVolumeInTransitAsset,
                    transitAssetMarketPrice.Price, market.TransitPairAssetBalance.Free, transitAssetSide);

                if (Convert.ToDouble(transitTradeVolume) < market.TransitMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on IndirectMarket {@transitMarket} -> {@targetMarket}. " +
                        "TransitTradeVolume is less than MarketMinVolume: {@tradeVolume} < {@minVolume}",
                        market.TransitMarketInfo.Market, market.TargetMarketInfo.Market,
                        transitTradeVolume, market.TransitMarketInfo.MinVolume);
                    continue;
                }

                var volumeInTransitAssetAfterTransitTrade = transitAssetSide == OrderSide.Buy
                    ? Truncate(transitTradeVolume, market.TransitMarketInfo.VolumeAccuracy) /
                      transitAssetMarketPrice.Price
                    : Truncate(transitTradeVolume, market.TransitMarketInfo.VolumeAccuracy) *
                      transitAssetMarketPrice.Price;
                var availableVolumeInTransitAssetAfterTransitTrade =
                    volumeInTransitAssetAfterTransitTrade + market.TargetPairAssetBalance.Free;
                var targetTradeVolume = GetTradeVolume(remainingVolumeToTradeInTargetAsset,
                    targetAssetMarketPrice.Price, availableVolumeInTransitAssetAfterTransitTrade,
                    targetAssetSide);

                if (Convert.ToDouble(targetTradeVolume) < market.TargetMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on IndirectMarket {@transitMarket} -> {@targetMarket}. " +
                        "TargetTradeVolume after TransitTrade will be less than MarketMinVolume: {@tradeVolume} < {@minVolume}",
                        market.TransitMarketInfo.Market, market.TargetMarketInfo.Market,
                        targetTradeVolume, market.TargetMarketInfo.MinVolume);
                    continue;
                }

                var transitTrade = await MakeMarketTradeAsync(transitTradeVolume, transitAssetSide,
                    market.TransitMarketInfo, market.ExchangeName, hedgeOperation.Id);
                hedgeOperation.AddTrade(transitTrade);

                var targetTrade = await MakeMarketTradeAsync(targetTradeVolume, targetAssetSide,
                    market.TargetMarketInfo, market.ExchangeName, hedgeOperation.Id);
                hedgeOperation.AddTrade(targetTrade);
            }
        }

        private OrderSide GetOrderSide(ExchangeMarketInfo market, string buyAsset)
        {
            if (market.BaseAsset == buyAsset)
            {
                return OrderSide.Buy;
            }

            if (market.QuoteAsset == buyAsset)
            {
                return OrderSide.Sell;
            }

            return OrderSide.UnknownOrderSide;
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

            return volumeAccuracy == null ? tradeVolume : Truncate(targetVolume, volumeAccuracy.Value);
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

            _logger.LogInformation("Made Trade. Request: {@request} Response: {@response}", request, response);

            return hedgeTrade;
        }

        private decimal Truncate(decimal value, int precision)
        {
            var step = (decimal)Math.Pow(10, precision);
            var tmp = Math.Truncate(step * value);

            return tmp / step;
        }

        private async Task<ICollection<HedgeTrade>> MakeLimitTradesAsync(decimal tradeVolume, OrderSide orderSide,
            ExchangeMarketInfo marketInfo, string exchangeName, string operationId, decimal price,
            IEnumerable<LimitTradeStep> steps)
        {
            var trades = new List<HedgeTrade>();
            var tradedVolume = 0m;

            foreach (var step in steps)
            {
                var currentPrice = _currentPricesCache.Get(marketInfo.Market, exchangeName);
                var priceIncreasedOnLimit = price * step.PriceIncreasePercentLimit / 100 + price < currentPrice.Price;

                if (priceIncreasedOnLimit)
                {
                    _logger.LogInformation("Price hit limit. CurrentPrice={@price} Limit={@limit}",
                        currentPrice.Price, step.PriceIncreasePercentLimit);
                }

                var request = new MakeLimitTradeRequest
                {
                    Side = orderSide,
                    Market = marketInfo.Market,
                    Volume = tradeVolume - tradedVolume,
                    ExchangeName = exchangeName,
                    ReferenceId = Guid.NewGuid().ToString(),
                    PriceLimit = priceIncreasedOnLimit
                        ? price * step.PriceIncrementPercentOnLimitHit / 100 + price
                        : price * step.PriceIncreasePercentLimit / 100 + price,
                    TimeLimit = step.TimeLimit
                };

                if (request.Volume < Convert.ToDecimal(marketInfo.MinVolume))
                {
                    _logger.LogInformation(
                        "Can't make LimitTrade. Trade Volume < MarketMinVolume: {@volume} {@minVolume}",
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
                    Price = Convert.ToDecimal(response.Price),
                    Id = response.ReferenceId ?? request.ReferenceId,
                    CreatedDate = DateTime.UtcNow,
                    ExternalId = response.Id,
                    FeeAsset = response.FeeSymbol,
                    FeeVolume = Math.Abs(Convert.ToDecimal(response.FeeVolume)),
                    Market = response.Market,
                    Side = response.Side,
                    Type = OrderType.Limit
                };

                _logger.LogInformation("Made LimitTrade. Request: {@request} Response: {@response}", request, response);

                trades.Add(hedgeTrade);
                tradedVolume += hedgeTrade.GetTradedVolume();

                if (tradedVolume >= Truncate(tradeVolume, marketInfo.VolumeAccuracy))
                {
                    break;
                }
            }

            return trades;
        }
    }
}