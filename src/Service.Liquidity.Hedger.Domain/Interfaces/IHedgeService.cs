using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeService
    {
        Task<HedgeOperation> HedgeAsync(HedgeInstruction hedgeInstruction);
    }
}