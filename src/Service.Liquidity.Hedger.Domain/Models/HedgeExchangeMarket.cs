using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.Liquidity.Hedger.Domain.Models
{
    public class HedgeExchangeMarket
    {
        public decimal Weight { get; set; }
        public string ExchangeName { get; set; }
        public ExchangeMarketInfo ExchangeMarketInfo { get; set; }
        public ExchangeBalance QuoteAssetExchangeBalance { get; set; }
    }
}