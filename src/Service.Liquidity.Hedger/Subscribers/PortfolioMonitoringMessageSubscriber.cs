using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Monitoring.Domain.Models;

namespace Service.Liquidity.Hedger.Subscribers
{
    public class PortfolioMonitoringMessageSubscriber : IStartable
    {
        private readonly ILogger<PortfolioMonitoringMessageSubscriber> _logger;
        private readonly ISubscriber<PortfolioMonitoringMessage> _subscriber;
        private readonly IHedgeService _hedgeService;

        public PortfolioMonitoringMessageSubscriber(
            ILogger<PortfolioMonitoringMessageSubscriber> logger,
            ISubscriber<PortfolioMonitoringMessage> subscriber,
            IHedgeService hedgeService
        )
        {
            _logger = logger;
            _subscriber = subscriber;
            _hedgeService = hedgeService;
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

                await _hedgeService.HedgeAsync(message.RuleSets, message.Checks, message.Portfolio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle {@message}", message);
            }
        }
    }
}