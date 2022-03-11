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
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Monitoring.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.Monitoring.Domain.Models.Hedging;
using Service.Liquidity.Monitoring.Domain.Models.Hedging.Common;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services
{
    public class HedgeService : IHedgeService
    {
        private readonly ILogger<HedgeService> _logger;
        private readonly IExternalMarket _externalMarket;
        private readonly IServiceBusPublisher<HedgeTradeMessage> _publisher;
        private readonly IHedgeStampStorage _hedgeStampStorage;
        private readonly ICurrentPricesCache _currentPricesCache;
        private readonly IHedgeStrategiesFactory _hedgeStrategiesFactory;
        private const string ExchangeName = "FTX";
        private static HedgeStamp _lastHedgeStamp;

        public HedgeService(
            ILogger<HedgeService> logger,
            IExternalMarket externalMarket,
            IServiceBusPublisher<HedgeTradeMessage> publisher,
            IHedgeStampStorage hedgeStampStorage,
            ICurrentPricesCache currentPricesCache,
            IHedgeStrategiesFactory hedgeStrategiesFactory
        )
        {
            _logger = logger;
            _externalMarket = externalMarket;
            _publisher = publisher;
            _hedgeStampStorage = hedgeStampStorage;
            _currentPricesCache = currentPricesCache;
            _hedgeStrategiesFactory = hedgeStrategiesFactory;
        }

        public async Task HedgeAsync(PortfolioMonitoringMessage message)
        {
            _lastHedgeStamp ??= await _hedgeStampStorage.GetAsync();

            if (_lastHedgeStamp == null)
            {
                _lastHedgeStamp ??= new HedgeStamp();
                await _hedgeStampStorage.AddOrUpdateAsync(_lastHedgeStamp);
            }

            if (message.Portfolio.HedgeStamp != null && message.Portfolio.HedgeStamp < _lastHedgeStamp.Value)
            {
                _logger.LogWarning("Hedge is skipped. Portfolio hedge stamp less than last hedge stamp");
                return;
            }

            var hightestPriorityRule = message.RuleSets
                .Where(rs => rs.NeedsHedging())
                .SelectMany(rs => rs.Rules)
                .Where(rule => rule.CurrentState.HedgeParams.Validate(out _))
                .MaxBy(r => r.CurrentState.HedgeParams.BuyVolume);

            if (hightestPriorityRule == null)
            {
                _logger.LogWarning("No rule for hedging");
                return;
            }

            if (!hightestPriorityRule.CurrentState.HedgeParams.Validate(out var errors))
            {
                _logger.LogWarning(
                    $"Hedging is skipped. Found Rule {hightestPriorityRule.Name} with invalid HedgeParams {string.Join(", ", errors)}");
                return;
            }

            await TradeAsync(hightestPriorityRule.CurrentState.HedgeParams);
        }
        
        private async Task RefreshStateAsync(MonitoringRuleSet ruleSet, PortfolioCheck[] checks,
            Portfolio portfolio)
        {
            foreach (var rule in ruleSet.Rules ?? Array.Empty<MonitoringRule>())
            {
                var strategy = _hedgeStrategiesFactory.Get(rule.HedgeStrategyType);
                //var hedgeParams = strategy.CalculateHedgeParams(portfolio, checks, rule.HedgeStrategyParams);
            }
        }

        private async Task TradeAsync(HedgeParams hedgeParams)
        {
            var balancesResp = await _externalMarket.GetBalancesAsync(new GetBalancesRequest
            {
                ExchangeName = ExchangeName
            });
            var marketInfosResp = await _externalMarket.GetMarketInfoListAsync(new GetMarketInfoListRequest
            {
                ExchangeName = ExchangeName
            });
            var hedgeTradeParamsList = new List<HedgeTradeParams>();

            foreach (var sellAsset in hedgeParams.SellAssets)
            {
                var marketInfo = marketInfosResp.Infos
                    .FirstOrDefault(m => m.BaseAsset == hedgeParams.BuyAssetSymbol && m.QuoteAsset == sellAsset.Symbol);
                var exchangeBalance = balancesResp.Balances.FirstOrDefault(b => b.Symbol == sellAsset.Symbol);

                if (marketInfo == null || exchangeBalance == null)
                {
                    _logger.LogWarning("SellAsset {@sellAsset} is skipped. {@marketInfo} {@exchangeBalance}",
                        sellAsset, marketInfo, exchangeBalance);
                    continue;
                }

                hedgeTradeParamsList.Add(new HedgeTradeParams
                {
                    Weight = sellAsset.Weight,
                    ExchangeBalance = exchangeBalance,
                    ExchangeMarketInfo = marketInfo
                });
            }

            if (!hedgeTradeParamsList.Any())
            {
                _logger.LogWarning($"Can't make hedge trade. Possible sell assets not found");
                return;
            }

            decimal tradedVolume = 0;
            var useBalancePercent = 0.9m;

            foreach (var hedgeTradeParams in hedgeTradeParamsList)
            {
                var currentPrice = _currentPricesCache.Get(ExchangeName, hedgeTradeParams.ExchangeMarketInfo.Market);
                var possibleVolume = hedgeTradeParams.ExchangeBalance.Free * currentPrice.Price * useBalancePercent;
                var tradeRequest = new MarketTradeRequest
                {
                    Side = OrderSide.Buy,
                    Market = hedgeTradeParams.ExchangeMarketInfo.Market,
                    Volume = Convert.ToDouble(possibleVolume),
                    ExchangeName = ExchangeName,
                    OppositeVolume = 0,
                    ReferenceId = null,
                };

                var tradeResp = new ExchangeTrade(); //await _externalMarket.MarketTrade(tradeRequest);

                _lastHedgeStamp.Increase();
                await _publisher.PublishAsync(new HedgeTradeMessage
                {
                    BaseAsset = hedgeTradeParams.ExchangeMarketInfo.BaseAsset,
                    BaseVolume = Convert.ToDecimal(tradeResp.Volume),
                    ExchangeName = tradeRequest.ExchangeName,
                    HedgeStamp = _lastHedgeStamp.Value,
                    QuoteAsset = hedgeTradeParams.ExchangeMarketInfo.QuoteAsset,
                    QuoteVolume = Convert.ToDecimal(tradeResp.Price * tradeResp.Volume),
                    Price = Convert.ToDecimal(tradeResp.Price),
                    Id = tradeResp.ReferenceId
                });
                await _hedgeStampStorage.AddOrUpdateAsync(_lastHedgeStamp);

                tradedVolume += Convert.ToDecimal(tradeResp.Volume);

                if (tradedVolume >= hedgeParams.BuyVolume)
                {
                    _logger.LogInformation(
                        $"Hedge ended. TradedVolume={tradedVolume} TargetVolume={hedgeParams.BuyVolume}");
                    return;
                }
            }
        }
    }

    public class HedgeTradeParams
    {
        public decimal Weight { get; set; }
        public ExchangeMarketInfo ExchangeMarketInfo { get; set; }
        public ExchangeBalance ExchangeBalance { get; set; }
    }
}