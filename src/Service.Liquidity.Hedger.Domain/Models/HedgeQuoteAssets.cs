using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeQuoteAssets
    {
        [DataMember(Order = 1)] public decimal Weight { get; set; }
        [DataMember(Order = 2)] public string Symbol { get; set; }
    }
}