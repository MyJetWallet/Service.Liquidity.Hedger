using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces;

public interface IMarketsAnalyzer
{
    Task<ICollection<HedgeExchangeMarket>> FindPossibleAsync(HedgeInstruction hedgeInstruction);
}