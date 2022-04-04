using Autofac;
using MyJetWallet.Domain.ExternalMarketApi;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Services;
using Service.Liquidity.Hedger.Jobs;
using Service.Liquidity.Hedger.NoSql;
using Service.Liquidity.Hedger.NoSql.HedgeInstructions;
using Service.Liquidity.Hedger.NoSql.Settings;
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
            builder.RegisterType<HedgeJob>().As<IStartable>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<HedgeService>().As<IHedgeService>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<HedgeOperationsNoSqlStorage>().As<IHedgeOperationsStorage>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<CurrentPricesNoSqlCache>().As<ICurrentPricesCache>().As<IStartable>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<HedgeStrategiesFactory>().As<IHedgeStrategiesFactory>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<ExchangesAnalyzer>().As<IExchangesAnalyzer>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<PortfolioAnalyzer>().As<IPortfolioAnalyzer>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<HedgeSettingsNoSqlStorage>().As<IHedgeSettingsStorage>()
                .AutoActivate().SingleInstance();
            builder.RegisterType<HedgeInstructionsNoSqlStorage>().As<IHedgeInstructionsStorage>()
                .AutoActivate().SingleInstance();
        }
    }
}