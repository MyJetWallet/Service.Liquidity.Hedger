using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeService
    {
        public Task HedgeAsync(ICollection<MonitoringRuleSet> ruleSets, ICollection<PortfolioCheck> checks,
            Portfolio portfolio);
    }
}