using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Hedger.Domain.Extensions;

public static class ExchangeMarketInfoExtensions
{
    public static OrderSide GetOrderSide(this ExchangeMarketInfo market, string buyAsset)
    {
        if (market.BaseAsset == buyAsset)
        {
            return OrderSide.Buy;
        }

        if (market.QuoteAsset == buyAsset)
        {
            return OrderSide.Sell;
        }

        return OrderSide.UnknownOrderSide;
    }
}