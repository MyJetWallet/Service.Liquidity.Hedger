using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgePairAsset
    {
        [DataMember(Order = 1)] public decimal Weight { get; set; }
        [DataMember(Order = 2)] public string Symbol { get; set; }
        [DataMember(Order = 3)] public decimal AvailableVolume { get; set; }
    }
}