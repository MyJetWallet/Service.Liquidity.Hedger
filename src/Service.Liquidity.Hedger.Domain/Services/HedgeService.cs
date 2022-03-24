using System;
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
        private const decimal BalancePercentToTrade = 0.9m;

        public HedgeService(
            ILogger<HedgeService> logger,
            IExternalMarket externalMarket,
            IHedgeOperationsStorage hedgeOperationsStorage,
            ICurrentPricesCache currentPricesCache,
            IExchangesAnalyzer exchangesAnalyzer
        )
        {
            _logger = logger;
            _externalMarket = externalMarket;
            _hedgeOperationsStorage = hedgeOperationsStorage;
            _currentPricesCache = currentPricesCache;
            _exchangesAnalyzer = exchangesAnalyzer;
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

            var possibleMarkets = await _exchangesAnalyzer.FindPossibleMarketsAsync(hedgeInstruction);

            foreach (var market in possibleMarkets)
            {
                var remainingVolumeToTrade = hedgeInstruction.TargetVolume - hedgeOperation.TradedVolume;
                var marketPrice = _currentPricesCache.Get(market.ExchangeName, market.Info.Market);
                var side = DetermineOrderSide(market.Info, hedgeInstruction.TargetAssetSymbol);
                var tradeVolume = CalculateTradeVolume(remainingVolumeToTrade, marketPrice.Price, market.Balance.Free, side);

                if (Convert.ToDouble(tradeVolume) < market.Info.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on market {@market}. VolumeToBuy {@volumeToBuy} < MarketMinVolume {@minVolume}",
                        market.Info.Market, tradeVolume, market.Info.MinVolume);
                    continue;
                }

                var trade = await TradeAsync(tradeVolume, side, market, hedgeOperation.Id);
                hedgeOperation.HedgeTrades.Add(trade);
                hedgeOperation.TradedVolume += side == OrderSide.Buy
                    ? Convert.ToDecimal(trade.BaseVolume)
                    : Convert.ToDecimal(trade.QuoteVolume);

                if (hedgeOperation.TradedVolume >= hedgeInstruction.TargetVolume)
                {
                    break;
                }
            }

            _logger.LogInformation("HedgeOperation ended. {@operation}", hedgeOperation);

            if (hedgeOperation.HedgeTrades.Any())
            {
                await _hedgeOperationsStorage.AddOrUpdateLastAsync(hedgeOperation);
            }

            return hedgeOperation;
        }

        private OrderSide DetermineOrderSide(ExchangeMarketInfo market, string buyAsset)
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

        private decimal CalculateTradeVolume(decimal targetVolume, decimal price, decimal balance, OrderSide side)
        {
            var availableVolumeOnBalance = balance * BalancePercentToTrade;

            if (side == OrderSide.Buy)
            {
                var possibleVolumeToBuy = availableVolumeOnBalance / price;
                return possibleVolumeToBuy < targetVolume
                    ? possibleVolumeToBuy // trade max possible volume
                    : targetVolume;
            }

            if (side == OrderSide.Sell)
            {
                var neededVolumeToSell = targetVolume / price;
                return availableVolumeOnBalance < neededVolumeToSell
                    ? availableVolumeOnBalance // trade max possible volume
                    : neededVolumeToSell;
            }
            
            return 0;
        }

        private async Task<HedgeTrade> TradeAsync(decimal tradeVolume, OrderSide orderSide,
            HedgeExchangeMarket market, string operationId)
        {
            var request = new MarketTradeRequest
            {
                Side = orderSide,
                Market = market.Info.Market,
                Volume = Convert.ToDouble(tradeVolume),
                ExchangeName = market.ExchangeName,
                OppositeVolume = 0,
                ReferenceId = Guid.NewGuid().ToString(),
            };

            var response = await _externalMarket.MarketTrade(request);

            var hedgeTrade = new HedgeTrade
            {
                BaseAsset = market.Info.BaseAsset,
                BaseVolume = Math.Abs(Convert.ToDecimal(response.Volume)),
                ExchangeName = request.ExchangeName,
                HedgeOperationId = operationId,
                QuoteAsset = market.Info.QuoteAsset,
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
    }
}