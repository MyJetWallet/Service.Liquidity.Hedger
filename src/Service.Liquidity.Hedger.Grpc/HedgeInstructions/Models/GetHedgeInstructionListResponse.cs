using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;

namespace Service.Liquidity.Hedger.Grpc.HedgeInstructions.Models
{
    [DataContract]
    public class GetHedgeInstructionListResponse
    {
        [DataMember(Order = 1)] public IEnumerable<HedgeInstruction> Items { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public bool IsError { get; set; }
    }
}