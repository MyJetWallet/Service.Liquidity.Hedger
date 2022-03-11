using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeStrategyParams
    {
        [DataMember(Order = 1)] public decimal AmountPercent { get; set; }
    }
}