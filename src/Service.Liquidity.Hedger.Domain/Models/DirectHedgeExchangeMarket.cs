using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using Service.Liquidity.Hedger.Domain.Extensions;

namespace Service.Liquidity.Hedger.Domain.Models
{
    public class DirectHedgeExchangeMarket
    {
        public decimal Weight { get; set; }
        public string ExchangeName { get; set; }
        public ExchangeMarketInfo Info { get; set; }
        public decimal AvailablePairAssetVolume { get; set; }
    }
}