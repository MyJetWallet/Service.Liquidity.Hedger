using System.Runtime.Serialization;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;

namespace Service.Liquidity.Hedger.Grpc.HedgeInstructions.Models
{
    [DataContract]
    public class AddOrUpdateHedgeInstructionRequest
    {
        [DataMember(Order = 1)] public HedgeInstruction Item { get; set; }
    }
}