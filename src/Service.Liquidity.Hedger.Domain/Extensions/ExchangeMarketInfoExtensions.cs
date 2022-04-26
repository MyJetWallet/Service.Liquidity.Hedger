using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Hedger.Domain.Extensions;

public static class ExchangeMarketInfoExtensions
{
    public static OrderSide GetOrderSideToBuyAsset(this ExchangeMarketInfo market, string buyAsset)
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
    
    public static OrderSide GetOrderSideToSellAsset(this ExchangeMarketInfo market, string sellAsset)
    {
        if (market.BaseAsset == sellAsset)
        {
            return OrderSide.Sell;
        }

        if (market.QuoteAsset == sellAsset)
        {
            return OrderSide.Buy;
        }

        return OrderSide.UnknownOrderSide;
    }
}