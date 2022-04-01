using System;
using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models;

[DataContract]
public class LimitTradeStep
{
    [DataMember(Order = 1)] public TimeSpan TimeLimit { get; set; }
    [DataMember(Order = 2)] public decimal PriceIncreasePercentLimit { get; set; }
    [DataMember(Order = 3)] public decimal PriceIncrementPercentOnLimitHit { get; set; }
    [DataMember(Order = 4)] public int Number { get; set; }
}