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
        private readonly IServiceBusPublisher<HedgeOperation> _publisher;
        private readonly IHedgeStampStorage _hedgeStampStorage;
        private readonly ICurrentPricesCache _currentPricesCache;
        private readonly IHedgeStrategiesFactory _hedgeStrategiesFactory;
        private readonly IMarketsAnalyzer _marketsAnalyzer;
        private const decimal BalancePercentToTrade = 0.9m;
        private static HedgeOperationId _lastOperationId;

        public HedgeService(
            ILogger<HedgeService> logger,
            IExternalMarket externalMarket,
            IServiceBusPublisher<HedgeOperation> publisher,
            IHedgeStampStorage hedgeStampStorage,
            ICurrentPricesCache currentPricesCache,
            IHedgeStrategiesFactory hedgeStrategiesFactory,
            IMarketsAnalyzer marketsAnalyzer
        )
        {
            _logger = logger;
            _externalMarket = externalMarket;
            _publisher = publisher;
            _hedgeStampStorage = hedgeStampStorage;
            _currentPricesCache = currentPricesCache;
            _hedgeStrategiesFactory = hedgeStrategiesFactory;
            _marketsAnalyzer = marketsAnalyzer;
        }

        public async Task HedgeAsync(ICollection<MonitoringRuleSet> ruleSets, ICollection<PortfolioCheck> checks,
            Portfolio portfolio)
        {
            _lastOperationId ??= await _hedgeStampStorage.GetAsync();

            if (_lastOperationId == null)
            {
                _lastOperationId ??= new HedgeOperationId();
                await _hedgeStampStorage.AddOrUpdateAsync(_lastOperationId);
            }

            if (portfolio.HedgeOperationId != null && portfolio.HedgeOperationId < _lastOperationId.Value)
            {
                _logger.LogWarning("Hedge is skipped. Portfolio hedge stamp less than last hedge stamp");
                return;
            }

            var hedgeInstruction = CalculateHedgeInstruction(ruleSets, checks, portfolio);

            if (hedgeInstruction == null)
            {
                _logger.LogWarning("No rule for hedging");
                return;
            }

            await HedgeAsync(hedgeInstruction);
        }

        private async Task HedgeAsync(HedgeInstruction hedgeInstruction)
        {
            var possibleMarkets = await _marketsAnalyzer.FindPossibleAsync(hedgeInstruction);

            if (!possibleMarkets.Any())
            {
                _logger.LogWarning("Can't hedge. Possible markets not found");
                return;
            }

            decimal tradedVolume = 0;
            var operationId = new HedgeOperationId();
            var trades = new List<HedgeTrade>(possibleMarkets.Count);

            foreach (var market in possibleMarkets)
            {
                var trade = await TradeAsync(operationId, market);
                trades.Add(trade);
                tradedVolume += Convert.ToDecimal(trade.BaseVolume);

                if (tradedVolume >= hedgeInstruction.BuyVolume)
                {
                    _logger.LogInformation(
                        $"Hedge ended. TradedVolume={tradedVolume} TargetVolume={hedgeInstruction.BuyVolume}");
                    return;
                }
            }
            
            await _publisher.PublishAsync(new HedgeOperation
            {
                Id = _lastOperationId.Value,
                TargetVolume = hedgeInstruction.BuyVolume,
                Trades = trades
            });
            _lastOperationId = operationId;
            await _hedgeStampStorage.AddOrUpdateAsync(_lastOperationId);
        }

        private async Task<HedgeTrade> TradeAsync(HedgeOperationId operationId, HedgeExchangeMarket market)
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
                OperationId = operationId.Value,
                QuoteAsset = market.ExchangeMarketInfo.QuoteAsset,
                QuoteVolume = Convert.ToDecimal(tradeResp.Price * tradeResp.Volume),
                Price = Convert.ToDecimal(tradeResp.Price),
                Id = tradeResp.ReferenceId
            };

            return hedgeTrade;
        }

        private HedgeInstruction CalculateHedgeInstruction(ICollection<MonitoringRuleSet> ruleSets,
            ICollection<PortfolioCheck> checks,
            Portfolio portfolio)
        {
            var hedgeInstructions = new List<HedgeInstruction>();

            foreach (var ruleSte in ruleSets?.Where(rs => rs.NeedsHedging()) ?? Array.Empty<MonitoringRuleSet>())
            {
                foreach (var rule in ruleSte.Rules?.Where(r => r.NeedsHedging()) ?? Array.Empty<MonitoringRule>())
                {
                    var ruleChecks = checks.Where(ch => rule.CheckIds.Contains(ch.Id));
                    var strategy = _hedgeStrategiesFactory.Get(rule.HedgeStrategyType);
                    var instruction = strategy.CalculateHedgeInstruction(portfolio, ruleChecks, rule.HedgeStrategyParams);
                    hedgeInstructions.Add(instruction);
                }
            }

            var highestPriorityInstruction = hedgeInstructions
                .Where(instruction => instruction.Validate(out _))
                .MaxBy(instruction => instruction.BuyVolume);

            return highestPriorityInstruction;
        }
    }
}