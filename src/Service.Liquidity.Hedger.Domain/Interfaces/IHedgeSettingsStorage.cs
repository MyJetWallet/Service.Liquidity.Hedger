using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces;

public interface IHedgeSettingsStorage
{
    Task AddOrUpdateAsync(HedgeSettings model);
    Task<HedgeSettings> GetAsync();
}