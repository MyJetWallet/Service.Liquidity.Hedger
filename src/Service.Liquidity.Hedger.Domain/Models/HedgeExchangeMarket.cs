using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.Liquidity.Hedger.Domain.Models
{
    public class HedgeExchangeMarket
    {
        public decimal Weight { get; set; }
        public string ExchangeName { get; set; }
        public ExchangeMarketInfo Info { get; set; }
        public ExchangeBalance Balance { get; set; }
    }
}