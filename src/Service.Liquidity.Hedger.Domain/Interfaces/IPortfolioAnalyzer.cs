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
        Task<HedgeInstruction> CalculateHedgeInstructionAsync(Portfolio portfolio,
            ICollection<MonitoringRuleSet> ruleSets, ICollection<PortfolioCheck> checks);
    }
}