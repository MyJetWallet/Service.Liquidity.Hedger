using System;
using System.Linq;
using System.Threading.Tasks;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.Service;
using Service.Liquidity.Hedger.Domain.Interfaces;

namespace Service.Liquidity.Hedger.Domain.Services;

public class PricesService : IPricesService
{
    private readonly ICurrentPricesCache _currentPricesCache;
    private readonly IOrderBookSource _orderBookSource;

    public PricesService(
        ICurrentPricesCache currentPricesCache,
        IOrderBookSource orderBookSource
        )
    {
        _currentPricesCache = currentPricesCache;
        _orderBookSource = orderBookSource;
    }

    public Task<decimal> GetConvertPriceAsync(string exchangeName, string market)
    {
        var marketPrice = _currentPricesCache.Get(exchangeName, market);

        if ((marketPrice?.Price ?? 0) == 0)
        {
            throw new Exception(
                $"Price for {exchangeName} {market} not found in CurrentPricesCache");
        }

        return Task.FromResult(marketPrice.Price);

    }

    public async Task<decimal> GetLimitTradePriceAsync(string exchangeName, string market, OrderSide orderSide)
    {
        var orderBookResp = await _orderBookSource.GetOrderBookAsync(new MarketRequest
        {
            Market = market,
            ExchangeName = exchangeName
        });
        var level = orderSide == OrderSide.Buy
            ? orderBookResp?.OrderBook?.Bids?.MaxBy(b => b.Price)
            : orderBookResp?.OrderBook?.Asks?.MinBy(b => b.Price);
        var levelPrice = level?.Price ?? 0;

        if (levelPrice == 0)
        {
            throw new Exception(
                $"Price for {exchangeName} {market} not found in OrderBook. {orderBookResp.ToJson()}");
        }

        return Convert.ToDecimal(levelPrice);
    }
}