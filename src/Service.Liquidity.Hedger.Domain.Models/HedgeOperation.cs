using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeOperation
    {
        public const string TopicName = "jetwallet-liquidity-hedge-operation";

        [DataMember(Order = 1)] public string Id { get; set; }
        [DataMember(Order = 2)] public List<HedgeTrade> HedgeTrades { get; set; } = new List<HedgeTrade>();
        [DataMember(Order = 3)] public decimal TargetVolume { get; set; }
        [DataMember(Order = 4)] public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [DataMember(Order = 5)] public string TargetAsset { get; set; }
        [DataMember(Order = 6)] public decimal TradedVolume { get; set; }

        public void AddTrade(HedgeTrade trade)
        {
            HedgeTrades ??= new List<HedgeTrade>();
            HedgeTrades.Add(trade);

            if (trade.QuoteAsset == TargetAsset || trade.BaseAsset == TargetAsset)
            {
                TradedVolume += trade.GetTradedVolume();
            }
        }

        public void AddTrades(IEnumerable<HedgeTrade> trades)
        {
            HedgeTrades ??= new List<HedgeTrade>();

            foreach (var trade in trades ?? Array.Empty<HedgeTrade>())
            {
                HedgeTrades.Add(trade);

                if (trade.QuoteAsset == TargetAsset || trade.BaseAsset == TargetAsset)
                {
                    TradedVolume += trade.GetTradedVolume();
                }
            }
        }

        public bool IsFullyHedged()
        {
            return TradedVolume >= TargetVolume;
        }
    }
}