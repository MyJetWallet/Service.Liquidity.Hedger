using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;

namespace Service.Liquidity.Hedger.Client
{
    [UsedImplicitly]
    public class LiquidityHedgerClientFactory : MyGrpcClientFactory
    {
        public LiquidityHedgerClientFactory(string grpcServiceUrl) :
            base(grpcServiceUrl)
        {
        }
    }
}