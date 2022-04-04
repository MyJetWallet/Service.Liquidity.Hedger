using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models;

[DataContract]
public class HedgeSettings
{
    [DataMember(Order = 1)] public ICollection<string> EnabledExchanges { get; set; } = new List<string>();
    [DataMember(Order = 2)] public ICollection<string> IndirectMarketTransitAssets { get; set; } = new List<string>();

    [DataMember(Order = 3)]
    public ICollection<LimitTradeStep> DirectMarketLimitTradeSteps { get; set; } = new List<LimitTradeStep>();

    [DataMember(Order = 4)]
    public ICollection<LimitTradeStep> IndirectMarketLimitTradeSteps { get; set; } = new List<LimitTradeStep>();
    
    [DataMember(Order = 5)] public bool ConfirmRequired { get; set; }
}