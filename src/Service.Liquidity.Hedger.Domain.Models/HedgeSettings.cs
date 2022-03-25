using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models;

[DataContract]
public class HedgeSettings
{
    [DataMember(Order = 1)] public bool IsEnabled { get; set; }
    [DataMember(Order = 2)] public ICollection<HedgePairAsset> TransitAssets { get; set; }
    [DataMember(Order = 3)] public ICollection<HedgeExchange> EnabledExchanges { get; set; }
}