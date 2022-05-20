using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models;

[DataContract]
public class PendingHedgeInstructionMessage
{
    public const string TopicName = "jetwallet-liquidity-pending-hedge-instruction";

    [DataMember(Order = 1)] public HedgeInstruction HedgeInstruction { get; set; }
    [DataMember(Order = 2)] public bool ConfirmRequired { get; set; }
}