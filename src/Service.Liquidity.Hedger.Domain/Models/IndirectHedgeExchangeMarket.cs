using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.Liquidity.Hedger.Domain.Models;

public class IndirectHedgeExchangeMarket
{
    public decimal Weight { get; set; }
    public string ExchangeName { get; set; }
    public string TransitAsset { get; set; }
    public ExchangeMarketInfo TransitAssetMarketInfo { get; set; }
    public ExchangeBalance TransitAssetAssetBalance { get; set; }
    public ExchangeMarketInfo TargetAssetMarketInfo { get; set; }
    public ExchangeBalance TargetAssetAssetBalance { get; set; }
}