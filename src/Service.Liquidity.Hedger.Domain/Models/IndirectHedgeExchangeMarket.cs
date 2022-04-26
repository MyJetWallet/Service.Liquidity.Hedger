using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.Liquidity.Hedger.Domain.Models;

public class IndirectHedgeExchangeMarket
{
    public decimal Weight { get; set; }
    public string ExchangeName { get; set; }
    public string TransitAssetSymbol { get; set; }
    public ExchangeMarketInfo FirstTradeMarketInfo { get; set; }
    public ExchangeMarketInfo SecondTradeMarketInfo { get; set; }
    public string PairAssetSymbol { get; set; }
    public decimal PairAssetAvailableVolume { get; set; }
    public decimal TargetAssetAvailableVolume { get; set; }

    public string GetMarketsDesc()
    {
        return $"{FirstTradeMarketInfo.Market} -> {SecondTradeMarketInfo.Market}";
    }
}