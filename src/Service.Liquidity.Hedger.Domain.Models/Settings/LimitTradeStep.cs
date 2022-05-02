using System;
using System.Runtime.Serialization;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Hedger.Domain.Models;

[DataContract]
public class LimitTradeStep
{
    [DataMember(Order = 1)] public TimeSpan DurationLimit { get; set; }
    [DataMember(Order = 2)] public decimal PriceChangePercentLimit { get; set; }
    [DataMember(Order = 3)] public decimal PriceChangePercentWhenLimitHit { get; set; }
    [DataMember(Order = 4)] public int Number { get; set; }

    public decimal CalculatePriceLimit(OrderSide orderSide, decimal initialPrice, decimal currentPrice)
    {
        bool priceChangedOnLimit;
        decimal priceLimit;
        var priceChange = initialPrice * PriceChangePercentLimit / 100;

        switch (orderSide)
        {
            case OrderSide.Buy:
            {
                if (currentPrice < initialPrice) // prevent buying by bigger price
                {
                    return currentPrice + currentPrice * PriceChangePercentLimit / 100;
                }
                
                var priceThreshold = initialPrice + priceChange;
                priceChangedOnLimit = priceThreshold < currentPrice; // increased more than on limit
                priceLimit = priceChangedOnLimit
                    ? initialPrice + initialPrice * PriceChangePercentWhenLimitHit / 100
                    : priceThreshold;
                break;
            }
            case OrderSide.Sell:
            {
                if (currentPrice > initialPrice) // prevent selling by smaller price
                {
                    return currentPrice - currentPrice * PriceChangePercentLimit / 100;
                }
                
                var priceThreshold = initialPrice - priceChange;
                priceChangedOnLimit = priceThreshold > currentPrice; // decreased more than on limit
                priceLimit = priceChangedOnLimit
                    ? initialPrice - initialPrice * PriceChangePercentWhenLimitHit / 100
                    : priceThreshold;
                break;
            }
            default:
                throw new NotSupportedException($"Order side {orderSide.ToString()}");
        }

        return priceLimit;
    }
}