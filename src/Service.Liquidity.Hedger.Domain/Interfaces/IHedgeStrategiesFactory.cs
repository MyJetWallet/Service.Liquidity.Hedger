using System.Collections.Generic;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeStrategiesFactory
    {
        IEnumerable<IHedgeStrategy> Get();
        IHedgeStrategy Get(HedgeStrategyType type);
    }
}