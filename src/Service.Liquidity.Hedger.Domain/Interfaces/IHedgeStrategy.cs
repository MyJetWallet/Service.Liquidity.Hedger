using System.Collections.Generic;
using System.Globalization;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeStrategy
    {
        Dictionary<string, string> ParamValuesByName { get; set; }

        HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, MonitoringRule rule);
    }
}