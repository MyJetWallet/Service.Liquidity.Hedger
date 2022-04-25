using Autofac;
using Service.Liquidity.Hedger.Grpc.HedgeInstructions;
using Service.Liquidity.Hedger.Grpc.HedgeSettings;
using Service.Liquidity.Monitoring.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.Liquidity.Hedger.Client
{
    public static class AutofacHelper
    {
        public static void RegisterLiquidityHedgerClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new LiquidityHedgerClientFactory(grpcServiceUrl);
            builder.RegisterInstance(factory.GetHedgeSettingsService()).As<IHedgeSettingsService>().SingleInstance();
            builder.RegisterInstance(factory.GetHedgeInstructionsService()).As<IHedgeInstructionsService>()
                .SingleInstance();
            builder.RegisterInstance(factory.GetHedgeMonitoringActionTemplatesService())
                .As<IMonitoringActionTemplatesService>()
                .SingleInstance();
        }
    }
}