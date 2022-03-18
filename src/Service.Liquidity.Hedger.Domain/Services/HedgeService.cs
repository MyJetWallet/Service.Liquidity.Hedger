using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.ServiceBus;
using Service.Liquidity.Hedger.Domain.Extensions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;
using Service.Liquidity.TradingPortfolio.Domain.Models;

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
                CreatedDate = DateTime.UtcNow
            };

            var possibleMarkets = await _exchangesAnalyzer.FindPossibleMarketsAsync(hedgeInstruction);

            if (!possibleMarkets.Any())
            {
                _logger.LogWarning("Can't hedge. Possible markets not found");
                return hedgeOperation;
            }

            decimal tradedVolume = 0;

            foreach (var market in possibleMarkets)
            {
                var marketPrice = _currentPricesCache.Get(market.ExchangeName, market.ExchangeMarketInfo.Market);
                var possibleVolumeToSell =
                    market.QuoteAssetExchangeBalance.Free / marketPrice.Price * BalancePercentToTrade;
                var remainingVolumeToBuy = hedgeInstruction.TargetVolume - tradedVolume;
                var volumeToBuy = possibleVolumeToSell < remainingVolumeToBuy
                    ? possibleVolumeToSell
                    : remainingVolumeToBuy;

                if (Convert.ToDouble(volumeToBuy) < market.ExchangeMarketInfo.MinVolume)
                {
                    _logger.LogWarning(
                        "Can't trade on market {@market}. VolumeToBuy {@volumeToBuy} < MarketMinVolume {@minVolume}",
                        market.ExchangeMarketInfo.Market, volumeToBuy, market.ExchangeMarketInfo.MinVolume);
                    continue;
                }

                var trade = await TradeAsync(volumeToBuy, market, hedgeOperation.Id);

                hedgeOperation.HedgeTrades.Add(trade);
                tradedVolume += Convert.ToDecimal(trade.BaseVolume);

                if (tradedVolume >= hedgeInstruction.TargetVolume)
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

        private async Task<HedgeTrade> TradeAsync(decimal targetVolume, HedgeExchangeMarket market, string operationId)
        {
            var request = new MarketTradeRequest
            {
                Side = OrderSide.Buy,
                Market = market.ExchangeMarketInfo.Market,
                Volume = Convert.ToDouble(targetVolume),
                ExchangeName = market.ExchangeName,
                OppositeVolume = 0,
                ReferenceId = Guid.NewGuid().ToString(),
            };

            var response = await _externalMarket.MarketTrade(request);

            var hedgeTrade = new HedgeTrade
            {
                BaseAsset = market.ExchangeMarketInfo.BaseAsset,
                BaseVolume = Convert.ToDecimal(response.Volume),
                ExchangeName = request.ExchangeName,
                HedgeOperationId = operationId,
                QuoteAsset = market.ExchangeMarketInfo.QuoteAsset,
                QuoteVolume = Convert.ToDecimal(response.Price * response.Volume),
                Price = Convert.ToDecimal(response.Price),
                Id = response.ReferenceId ?? request.ReferenceId,
                CreatedDate = DateTime.UtcNow,
                ExternalId = response.Id,
                FeeAsset = response.FeeSymbol,
                FeeVolume = Convert.ToDecimal(response.Volume)
            };
            
            _logger.LogInformation("Made Trade. Request: {@request} Response: {@response}", request, response);

            return hedgeTrade;
        }
    }
}