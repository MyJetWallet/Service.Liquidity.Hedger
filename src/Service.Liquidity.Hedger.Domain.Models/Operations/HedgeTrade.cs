using System;
using System.Runtime.Serialization;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeTrade
    {
        [DataMember(Order = 1)] public string HedgeOperationId { get; set; }
        [DataMember(Order = 2)] public string BaseAsset { get; set; }
        [DataMember(Order = 3)] public decimal BaseVolume { get; set; }
        [DataMember(Order = 4)] public string QuoteAsset { get; set; }
        [DataMember(Order = 5)] public decimal QuoteVolume { get; set; }
        [DataMember(Order = 6)] public string ExchangeName { get; set; }
        [DataMember(Order = 7)] public decimal Price { get; set; }
        [DataMember(Order = 8)] public string Id { get; set; }
        [DataMember(Order = 9)] public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [DataMember(Order = 10)] public string ExternalId { get; set; }
        [DataMember(Order = 11)] public string FeeAsset { get; set; }
        [DataMember(Order = 12)] public decimal FeeVolume { get; set; }
        [DataMember(Order = 13)] public string Market { get; set; }
        [DataMember(Order = 14)] public OrderSide Side { get; set; }
        [DataMember(Order = 15)] public OrderType Type { get; set; }

        public decimal GetTradedVolume(string asset)
        {
            if (BaseAsset == asset)
            {
                return BaseVolume;
            }

            if (QuoteAsset == asset)
            {
                return QuoteVolume;
            }
            
            return 0;
        }
    }
}