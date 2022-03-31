using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Grpc.HedgeSettings.Models;

[DataContract]
public class AddOrUpdateHedgeSettingResponse
{
    [DataMember(Order = 1)]public bool IsError { get; set; }
    [DataMember(Order = 2)]public string ErrorMessage { get; set; }
}