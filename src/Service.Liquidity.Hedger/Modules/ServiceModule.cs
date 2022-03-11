using Autofac;
using MyJetWallet.Domain.ExternalMarketApi;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Services;
using Service.Liquidity.Hedger.NoSql;
using Service.Liquidity.Hedger.Subscribers;

namespace Service.Liquidity.Hedger.Modules
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterExternalMarketClient(Program.Settings.ExternalApiGrpcUrl);

            builder.RegisterType<PortfolioMonitoringMessageSubscriber>().As<IStartable>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<HedgeService>().As<IHedgeService>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<HedgeStampNoSqlStorage>().As<IHedgeStampStorage>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<CurrentPricesNoSqlCache>().As<ICurrentPricesCache>().As<IStartable>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<HedgeStrategiesFactory>().As<IHedgeStrategiesFactory>()
                .AutoActivate().SingleInstance();
        }
    }
}