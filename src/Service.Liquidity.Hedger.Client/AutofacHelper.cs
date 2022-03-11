using Autofac;

// ReSharper disable UnusedMember.Global

namespace Service.Liquidity.Hedger.Client
{
    public static class AutofacHelper
    {
        public static void RegisterLiquidityHedgerClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new LiquidityHedgerClientFactory(grpcServiceUrl);
        }
    }
}