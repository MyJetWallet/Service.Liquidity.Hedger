using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeOperationsStorage
    {
        Task AddOrUpdateLastAsync(HedgeOperation model);
        Task<HedgeOperation> GetLastAsync();
    }
}