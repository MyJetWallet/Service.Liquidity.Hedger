using System.Collections.Generic;
using HedgeStrategyType = Service.Liquidity.Hedger.Domain.Models.HedgeStrategyType;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeStrategiesFactory
    {
        IEnumerable<IHedgeStrategy> Get();
        IHedgeStrategy Get(HedgeStrategyType type);
    }
}