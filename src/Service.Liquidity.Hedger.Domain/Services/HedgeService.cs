﻿using System;
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
        private readonly IMarketsAnalyzer _marketsAnalyzer;
        private const decimal BalancePercentToTrade = 0.9m;

        public HedgeService(
            ILogger<HedgeService> logger,
            IExternalMarket externalMarket,
            IHedgeOperationsStorage hedgeOperationsStorage,
            ICurrentPricesCache currentPricesCache,
            IMarketsAnalyzer marketsAnalyzer
        )
        {
            _logger = logger;
            _externalMarket = externalMarket;
            _hedgeOperationsStorage = hedgeOperationsStorage;
            _currentPricesCache = currentPricesCache;
            _marketsAnalyzer = marketsAnalyzer;
        }

        public async Task<HedgeOperation> HedgeAsync(HedgeInstruction hedgeInstruction)
        {
            var possibleMarkets = await _marketsAnalyzer.FindPossibleAsync(hedgeInstruction);

            if (!possibleMarkets.Any())
            {
                _logger.LogWarning("Can't hedge. Possible markets not found");
                return null;
            }

            decimal tradedVolume = 0;
            var hedgeOperation = new HedgeOperation
            {
                Id = DateTime.UtcNow.Ticks,
                TargetVolume = hedgeInstruction.BuyVolume,
                Trades = new List<HedgeTrade>(possibleMarkets.Count)
            };

            foreach (var market in possibleMarkets)
            {
                var trade = await TradeAsync(hedgeOperation.Id, market);
                hedgeOperation.Trades.Add(trade);
                tradedVolume += Convert.ToDecimal(trade.BaseVolume);

                if (tradedVolume >= hedgeInstruction.BuyVolume)
                {
                    break;
                }
            }

            _logger.LogInformation(
                $"Hedge ended. TradedVolume={tradedVolume} TargetVolume={hedgeInstruction.BuyVolume}");
            await _hedgeOperationsStorage.AddOrUpdateLastAsync(hedgeOperation);

            return hedgeOperation;
        }

        private async Task<HedgeTrade> TradeAsync(long operationId, HedgeExchangeMarket market)
        {
            var currentPrice = _currentPricesCache.Get(market.ExchangeName, market.ExchangeMarketInfo.Market);
            var possibleVolume = market.ExchangeBalance.Free * currentPrice.Price * BalancePercentToTrade;
            var tradeRequest = new MarketTradeRequest
            {
                Side = OrderSide.Buy,
                Market = market.ExchangeMarketInfo.Market,
                Volume = Convert.ToDouble(possibleVolume),
                ExchangeName = market.ExchangeName,
                OppositeVolume = 0,
                ReferenceId = Guid.NewGuid().ToString(),
            };

            var tradeResp = new ExchangeTrade(); //await _externalMarket.MarketTrade(tradeRequest);

            var hedgeTrade = new HedgeTrade
            {
                BaseAsset = market.ExchangeMarketInfo.BaseAsset,
                BaseVolume = Convert.ToDecimal(tradeResp.Volume),
                ExchangeName = tradeRequest.ExchangeName,
                OperationId = operationId,
                QuoteAsset = market.ExchangeMarketInfo.QuoteAsset,
                QuoteVolume = Convert.ToDecimal(tradeResp.Price * tradeResp.Volume),
                Price = Convert.ToDecimal(tradeResp.Price),
                Id = tradeResp.ReferenceId
            };

            return hedgeTrade;
        }
    }
}