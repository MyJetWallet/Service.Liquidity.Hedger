﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeOperation
    {
        public const string TopicName = "jetwallet-liquidity-hedge-operation";

        [DataMember(Order = 1)] public long Id { get; set; }
        [DataMember(Order = 2)] public List<HedgeTrade> Trades { get; set; } = new List<HedgeTrade>();
        [DataMember(Order = 3)] public decimal TargetVolume { get; set; }
    }
}