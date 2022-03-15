using System.Collections.Generic;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeStrategiesFactory
    {
        IEnumerable<IHedgeStrategy> Get();
        IHedgeStrategy Get(HedgeStrategyType type);
    }
}