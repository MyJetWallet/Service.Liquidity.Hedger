using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeStampStorage
    {
        Task AddOrUpdateAsync(HedgeOperationId model);
        Task<HedgeOperationId> GetAsync();
    }
}