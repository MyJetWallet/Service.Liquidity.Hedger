﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.ServiceBus;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models;

namespace Service.Liquidity.Hedger.Subscribers
{
    public class PortfolioMonitoringMessageSubscriber : IStartable
    {
        private readonly ILogger<PortfolioMonitoringMessageSubscriber> _logger;
        private readonly ISubscriber<PortfolioMonitoringMessage> _subscriber;
        private readonly IHedgeService _hedgeService;
        private readonly IPortfolioAnalyzer _portfolioAnalyzer;
        private readonly IServiceBusPublisher<HedgeOperation> _publisher;

        public PortfolioMonitoringMessageSubscriber(
            ILogger<PortfolioMonitoringMessageSubscriber> logger,
            ISubscriber<PortfolioMonitoringMessage> subscriber,
            IHedgeService hedgeService,
            IPortfolioAnalyzer portfolioAnalyzer,
            IServiceBusPublisher<HedgeOperation> publisher
        )
        {
            _logger = logger;
            _subscriber = subscriber;
            _hedgeService = hedgeService;
            _portfolioAnalyzer = portfolioAnalyzer;
            _publisher = publisher;
        }

        public void Start()
        {
            _subscriber.Subscribe(Handle);
        }

        private async ValueTask Handle(PortfolioMonitoringMessage message)
        {
            try
            {
                if (message.Portfolio == null)
                {
                    _logger.LogWarning("Received PortfolioMonitoringMessage without Portfolio");
                    return;
                }

                if (message.Checks == null || !message.Checks.Any())
                {
                    _logger.LogWarning("Received PortfolioMonitoringMessage without Checks");
                    return;
                }

                if (message.RuleSets == null || !message.RuleSets.Any())
                {
                    _logger.LogWarning("Received PortfolioMonitoringMessage without RuleSets");
                    return;
                }

                var hedgeInstruction = await _portfolioAnalyzer.CalculateHedgeInstructionAsync(message.Portfolio,
                    message.RuleSets, message.Checks);
                
                if (hedgeInstruction == null)
                {
                    _logger.LogWarning("Hedge is skipped. HedgeOperationId in Portfolio is less than last HedgeOperationId");
                    return;
                }
                
                var hedgeOperation = await _hedgeService.HedgeAsync(hedgeInstruction);

                if (hedgeOperation == null)
                {
                    return;
                }

                await _publisher.PublishAsync(hedgeOperation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle {@message}", message);
            }
        }
    }
}