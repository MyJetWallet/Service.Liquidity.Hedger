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
        HedgeInstruction GetHedgeInstruction(Portfolio portfolio,
            ICollection<MonitoringRuleSet> ruleSets, ICollection<PortfolioCheck> checks);

        Task<bool> NeedsHedging(Portfolio portfolio);
    }
}