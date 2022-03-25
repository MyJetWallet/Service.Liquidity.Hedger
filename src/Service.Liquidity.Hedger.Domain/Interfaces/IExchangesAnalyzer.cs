using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces;

public interface IExchangesAnalyzer
{
    Task<ICollection<HedgeExchangeMarket>> FindPossibleMarketsAsync(HedgeInstruction hedgeInstruction);

    Task<ICollection<IndirectHedgeExchangeMarket>> FindIndirectMarketsAsync(
        string transitAssetSymbol, string targetAssetSymbol, IEnumerable<HedgePairAsset> pairAssets);
}