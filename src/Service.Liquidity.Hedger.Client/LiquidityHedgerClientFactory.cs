using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Liquidity.Hedger.Grpc.HedgeSettings;

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
    }
}