using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces;

public interface IHedgeInstructionsStorage
{
    Task<IEnumerable<HedgeInstruction>> GetAsync();
    Task AddOrUpdateAsync(HedgeInstruction model);
    Task<HedgeInstruction> GetAsync(string monitoringRuleId);
    Task DeleteAsync(string monitoringRuleId);
    Task AddOrUpdateAsync(IEnumerable<HedgeInstruction> models);
}