using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeTrade
    {
        [DataMember(Order = 1)] public long OperationId { get; set; }
        [DataMember(Order = 2)] public string BaseAsset { get; set; }
        [DataMember(Order = 3)] public decimal BaseVolume { get; set; }
        [DataMember(Order = 4)] public string QuoteAsset { get; set; }
        [DataMember(Order = 5)] public decimal QuoteVolume { get; set; }
        [DataMember(Order = 6)] public string ExchangeName { get; set; }
        [DataMember(Order = 7)] public decimal Price { get; set; }
        [DataMember(Order = 8)] public string Id { get; set; }
    }
}