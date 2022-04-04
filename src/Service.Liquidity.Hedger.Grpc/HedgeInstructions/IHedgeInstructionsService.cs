using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Hedger.Grpc.HedgeInstructions.Models;

namespace Service.Liquidity.Hedger.Grpc.HedgeInstructions
{
    [ServiceContract]
    public interface IHedgeInstructionsService
    {
        [OperationContract]
        Task<GetHedgeInstructionListResponse> GetListAsync(GetHedgeInstructionListRequest request);

        [OperationContract]
        Task<AddOrUpdateHedgeInstructionResponse> AddOrUpdateAsync(AddOrUpdateHedgeInstructionRequest request);

        [OperationContract]
        Task<GetHedgeInstructionResponse> GetAsync(GetHedgeInstructionRequest request);

        [OperationContract]
        Task<DeleteHedgeInstructionResponse> DeleteAsync(DeleteHedgeInstructionRequest request);
    }
}