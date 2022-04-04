using System.Runtime.Serialization;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;

namespace Service.Liquidity.Hedger.Grpc.HedgeInstructions.Models
{
    [DataContract]
    public class GetHedgeInstructionResponse
    {
        [DataMember(Order = 1)] public HedgeInstruction Item { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public bool IsError { get; set; }
    }
}