using System.Collections.Generic;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
using Service.Liquidity.TradingPortfolio.Domain.Models;
using HedgeStrategyType = Service.Liquidity.Hedger.Domain.Models.HedgeStrategyType;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeStrategy
    {
        public HedgeStrategyType Type { get; set; }

        public HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, MonitoringRule rule,
            decimal hedgePercent);
    }
}