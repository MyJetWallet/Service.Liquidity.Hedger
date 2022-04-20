using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.Liquidity.Hedger.Domain.Models;

public class IndirectHedgeExchangeMarket
{
    public decimal Weight { get; set; }
    public string ExchangeName { get; set; }
    public string TransitAssetSymbol { get; set; }
    public ExchangeMarketInfo TransitMarketInfo { get; set; }
    public ExchangeMarketInfo TargetMarketInfo { get; set; }
    public string TransitPairAssetSymbol { get; set; }
}