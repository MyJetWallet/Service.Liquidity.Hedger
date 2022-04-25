using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Liquidity.Hedger.Grpc.HedgeInstructions;
using Service.Liquidity.Hedger.Grpc.HedgeSettings;
using Service.Liquidity.Monitoring.Grpc;

namespace Service.Liquidity.Hedger.Client
{
    [UsedImplicitly]
    public class LiquidityHedgerClientFactory : MyGrpcClientFactory
    {
        public LiquidityHedgerClientFactory(string grpcServiceUrl) :
            base(grpcServiceUrl)
        {
        }

        public IHedgeSettingsService GetHedgeSettingsService() => CreateGrpcService<IHedgeSettingsService>();
        public IHedgeInstructionsService GetHedgeInstructionsService() => CreateGrpcService<IHedgeInstructionsService>();
        public IMonitoringActionTemplatesService GetHedgeMonitoringActionTemplatesService() => CreateGrpcService<IMonitoringActionTemplatesService>();

    }
}