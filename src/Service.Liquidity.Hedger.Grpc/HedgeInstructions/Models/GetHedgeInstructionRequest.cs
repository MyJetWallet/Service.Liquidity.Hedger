using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Grpc.HedgeInstructions.Models
{
    [DataContract]
    public class GetHedgeInstructionRequest
    {
        [DataMember(Order = 1)] public string Id { get; set; }
    }
}