using Autofac;
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
                () => Program.Settings.SpotServiceBusHostPort,
                Program.LogFactory);

            builder.RegisterMyServiceBusPublisher<HedgeOperation>(serviceBusClient,
                HedgeOperation.TopicName, true);
            builder.RegisterMyServiceBusPublisher<ConfirmedHedgeInstruction>(serviceBusClient,
                ConfirmedHedgeInstruction.TopicName, false);
            builder.RegisterMyServiceBusPublisher<PendingHedgeInstructionMessage>(serviceBusClient,
                PendingHedgeInstructionMessage.TopicName, false);

            var queueName = "Liquidity-Hedger";
            builder.RegisterMyServiceBusSubscriberSingle<PortfolioMonitoringMessage>(serviceBusClient,
                PortfolioMonitoringMessage.TopicName,
                queueName,
                TopicQueueType.DeleteOnDisconnect);
        }
    }
}