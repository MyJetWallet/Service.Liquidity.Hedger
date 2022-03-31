using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Grpc.HedgeSettings.Models;

[DataContract]
public class AddOrUpdateHedgeSettingsRequest
{
    [DataMember(Order = 1)] public Domain.Models.HedgeSettings Item { get; set; }
}