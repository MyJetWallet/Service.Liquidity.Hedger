using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.Liquidity.Hedger.Domain.Models
{
    public class DirectHedgeExchangeMarket
    {
        public decimal Weight { get; set; }
        public string ExchangeName { get; set; }
        public ExchangeMarketInfo Info { get; set; }
        public decimal AvailableVolume { get; set; }
    }
}