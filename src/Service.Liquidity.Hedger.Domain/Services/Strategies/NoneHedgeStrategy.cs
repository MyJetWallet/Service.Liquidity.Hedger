using System.Collections.Generic;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Strategies
{
    public class NoneHedgeStrategy : IHedgeStrategy
    {
        public HedgeStrategyType Type { get; set; } = HedgeStrategyType.None;

        public HedgeInstruction CalculateHedgeParams(Portfolio portfolio, IEnumerable<PortfolioCheck> checks,
            HedgeStrategyParams strategyParams)
        {
            return new HedgeInstruction();
        }
    }
}