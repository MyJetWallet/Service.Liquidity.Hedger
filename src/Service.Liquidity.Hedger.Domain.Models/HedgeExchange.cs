using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models;

[DataContract]
public class HedgeExchange
{
    [DataMember(Order = 1)] public decimal Priority { get; set; }
    [DataMember(Order = 2)] public string Name { get; set; }
}