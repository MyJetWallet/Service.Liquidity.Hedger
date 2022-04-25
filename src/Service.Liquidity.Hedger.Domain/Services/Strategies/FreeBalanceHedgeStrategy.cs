using System.Collections.Generic;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services.Strategies;

public class FreeBalanceHedgeStrategy : IHedgeStrategy
{
    public Dictionary<string, string> ParamValuesByName { get; set; }

    public HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, MonitoringRule rule)
    {
        throw new System.NotImplementedException();
    }
}