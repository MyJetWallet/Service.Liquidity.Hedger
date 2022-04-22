using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces;

public interface IHedgeInstructionsCache
{
    Task<IEnumerable<HedgeInstruction>> GetAsync();
}