using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IPortfolioAnalyzer
    {
        Task<bool> TimeToHedge(Portfolio portfolio);
        ICollection<HedgeInstruction> CalculateHedgeInstructions(Portfolio portfolio, ICollection<MonitoringRule> rules);
        HedgeInstruction SelectPriorityInstruction(IEnumerable<HedgeInstruction> instructions);
        ICollection<MonitoringRule> SelectHedgeRules(ICollection<MonitoringRule> rules);
    }
}