using System.Collections.Generic;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.TradingPortfolio.Domain.Models;
using HedgeStrategyType = Service.Liquidity.Hedger.Domain.Models.HedgeStrategyType;

namespace Service.Liquidity.Hedger.Domain.Services.Strategies
{
    public class ReturnHedgeStrategy : IHedgeStrategy
    {
        public HedgeStrategyType Type { get; set; } = HedgeStrategyType.Return;

        public HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, IEnumerable<PortfolioCheck> checks,
            decimal hedgePercent)
        {
            return new HedgeInstruction();
        }
    }
}