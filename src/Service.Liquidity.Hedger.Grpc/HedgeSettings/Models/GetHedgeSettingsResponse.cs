using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Grpc.HedgeSettings.Models;

[DataContract]
public class GetHedgeSettingsResponse
{
    [DataMember(Order = 1)] public bool IsError { get; set; }
    [DataMember(Order = 2)] public string ErrorMessage { get; set; }
    [DataMember(Order = 3)] public Domain.Models.HedgeSettings Item { get; set; }
}