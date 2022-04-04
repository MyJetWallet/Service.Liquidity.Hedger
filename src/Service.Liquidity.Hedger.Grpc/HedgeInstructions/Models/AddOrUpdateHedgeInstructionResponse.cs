using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Grpc.HedgeInstructions.Models
{
    [DataContract]
    public class AddOrUpdateHedgeInstructionResponse
    {
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 1)] public bool IsError { get; set; }
    }
}