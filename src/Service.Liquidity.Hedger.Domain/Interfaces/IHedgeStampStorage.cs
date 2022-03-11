using System.Threading.Tasks;
using Service.Liquidity.Monitoring.Domain.Models.Hedging;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeStampStorage
    {
        Task AddOrUpdateAsync(HedgeStamp model);
        Task<HedgeStamp> GetAsync();
    }
}