using System.Collections.Generic;
using HedgeStrategyType = Service.Liquidity.Hedger.Domain.Models.HedgeStrategyType;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeStrategiesFactory
    {
        IHedgeStrategy Get(HedgeStrategyType type, Dictionary<string, string> paramValuesByName);
    }
}