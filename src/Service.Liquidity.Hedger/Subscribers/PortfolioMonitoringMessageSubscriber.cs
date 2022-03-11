using System;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Monitoring.Domain.Models;

namespace Service.Liquidity.Hedger.Subscribers
{
    public class PortfolioMonitoringMessageSubscriber : IStartable
    {
        private readonly ILogger<PortfolioMonitoringMessageSubscriber> _logger;
        private readonly ISubscriber<PortfolioMonitoringMessage> _subscriber;

        public PortfolioMonitoringMessageSubscriber(
            ILogger<PortfolioMonitoringMessageSubscriber> logger,
            ISubscriber<PortfolioMonitoringMessage> subscriber
        )
        {
            _logger = logger;
            _subscriber = subscriber;
        }

        public void Start()
        {
            _subscriber.Subscribe(Handle);
        }

        private async ValueTask Handle(PortfolioMonitoringMessage message)
        {
            try
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle {@message}", message);
            }
        }
    }
}