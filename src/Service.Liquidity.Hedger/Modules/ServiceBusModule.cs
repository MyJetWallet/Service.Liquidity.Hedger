﻿using Autofac;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models;

namespace Service.Liquidity.Hedger.Modules
{
    public class ServiceBusModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(
                Program.ReloadedSettings(e => e.SpotServiceBusHostPort),
                Program.LogFactory);

            builder.RegisterMyServiceBusPublisher<HedgeOperation>(serviceBusClient,
                HedgeOperation.TopicName, true);

            var queueName = "Liquidity-Hedger-local";
            builder.RegisterMyServiceBusSubscriberSingle<PortfolioMonitoringMessage>(serviceBusClient,
                PortfolioMonitoringMessage.TopicName,
                queueName,
                TopicQueueType.DeleteOnDisconnect);
        }
    }
}