using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Hedger.Grpc.HedgeSettings.Models;

namespace Service.Liquidity.Hedger.Grpc.HedgeSettings;

[ServiceContract]
public interface IHedgeSettingsService
{
    [OperationContract]
    Task<GetHedgeSettingsResponse> GetAsync(GetHedgeSettingRequest request);

    [OperationContract]
    Task<AddOrUpdateHedgeSettingResponse> AddOrUpdateAsync(AddOrUpdateHedgeSettingsRequest request);
}