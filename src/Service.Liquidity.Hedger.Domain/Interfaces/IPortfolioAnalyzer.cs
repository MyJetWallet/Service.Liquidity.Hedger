using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IPortfolioAnalyzer
    {
        Task<bool> TimeToHedge(Portfolio portfolio);
        ICollection<MonitoringRule> SelectHedgeRules(ICollection<MonitoringRuleSet> ruleSets);

        ICollection<HedgeInstruction> CalculateHedgeInstructions(Portfolio portfolio,
            ICollection<MonitoringRule> rules, ICollection<PortfolioCheck> checks);

        HedgeInstruction SelectPriorityInstruction(IEnumerable<HedgeInstruction> instructions);
    }
}