using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models;

namespace Service.Liquidity.Hedger.Subscribers
{
    public class PortfolioMonitoringMessageSubscriber : IStartable
    {
        private readonly ILogger<PortfolioMonitoringMessageSubscriber> _logger;
        private readonly ISubscriber<PortfolioMonitoringMessage> _subscriber;
        private readonly IPortfolioAnalyzer _portfolioAnalyzer;
        private readonly IHedgeInstructionsStorage _hedgeInstructionsStorage;
        private readonly IHedgeInstructionsCache _hedgeInstructionsCache;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public PortfolioMonitoringMessageSubscriber(
            ILogger<PortfolioMonitoringMessageSubscriber> logger,
            ISubscriber<PortfolioMonitoringMessage> subscriber,
            IPortfolioAnalyzer portfolioAnalyzer,
            IHedgeInstructionsStorage hedgeInstructionsStorage,
            IHedgeInstructionsCache hedgeInstructionsCache
        )
        {
            _logger = logger;
            _subscriber = subscriber;
            _portfolioAnalyzer = portfolioAnalyzer;
            _hedgeInstructionsStorage = hedgeInstructionsStorage;
            _hedgeInstructionsCache = hedgeInstructionsCache;
        }

        public void Start()
        {
            _subscriber.Subscribe(Handle);
        }

        private async ValueTask Handle(PortfolioMonitoringMessage message)
        {
            var isHandleStarted = false;
            try
            {
                if (_semaphore.CurrentCount == 0)
                {
                    return;
                }

                await _semaphore.WaitAsync();
                isHandleStarted = true;

                if (message.Portfolio == null)
                {
                    _logger.LogWarning("Received {@Message} without Portfolio", nameof(PortfolioMonitoringMessage));
                    return;
                }

                if (message.Rules == null || !message.Rules.Any())
                {
                    _logger.LogWarning("Received {@Message} without Rules", nameof(PortfolioMonitoringMessage));
                    return;
                }

                var stopActionType = new StopHedgeMonitoringAction().TypeName;
                var stopHedgeRule = message.Rules.FirstOrDefault(rule => (rule.CurrentState?.IsActive ?? false) &&
                                                                         rule.ActionsByTypeName != null &&
                                                                         rule.ActionsByTypeName.TryGetValue(
                                                                             stopActionType, out _));

                if (stopHedgeRule != null)
                {
                    _logger.LogWarning("Hedge is stopped. Found stop hedge rule {@Rule}", stopHedgeRule.Name);
                    return;
                }

                if (await _portfolioAnalyzer.TimeToHedge(message.Portfolio))
                {
                    var savedInstructions = (await _hedgeInstructionsCache.GetAsync())?
                        .ToList() ?? new List<HedgeInstruction>();
                    var savedRuleIds = savedInstructions
                        .Select(i => i.MonitoringRuleId)
                        .ToHashSet();;
                    var pendingRuleIds = savedInstructions
                        .Where(i => i.Status == HedgeInstructionStatus.Pending)
                        .Select(i => i.MonitoringRuleId)
                        .ToHashSet();
                    var needCalculationRules = _portfolioAnalyzer
                        .SelectHedgeRules(message.Rules)
                        .Where(r => pendingRuleIds.Contains(r.Id) || !savedRuleIds.Contains(r.Id))
                        .ToList();
                    var recalculatedInstructions = _portfolioAnalyzer.CalculateHedgeInstructions(
                        message.Portfolio, needCalculationRules);
                    
                    foreach (var pendingRuleId in pendingRuleIds)
                    {
                        await _hedgeInstructionsStorage.DeleteAsync(pendingRuleId);
                    }
                    
                    await _hedgeInstructionsStorage.AddOrUpdateAsync(recalculatedInstructions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle {@Message}. {@ExMessage}", nameof(PortfolioMonitoringMessage),
                    ex.Message);
            }
            finally
            {
                if (isHandleStarted)
                {
                    _semaphore.Release();
                }
            }
        }
    }
}