using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models;

[DataContract]
public class ConfirmedHedgeInstruction
{
    public const string TopicName = "jetwallet-liquidity-confirmed-hedge-instruction";

    [DataMember(Order = 1)] public HedgeInstruction HedgeInstruction { get; set; }
}