using System.Linq;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;

namespace Service.Liquidity.Hedger.Domain.Extensions
{
    public static class MonitoringRuleSetExtensions
    {
        public static bool NeedsHedging(this MonitoringRuleSet ruleSet)
        {
            if (ruleSet.Rules == null || !ruleSet.Rules.Any())
            {
                return false;
            }

            var activeRules = ruleSet.Rules.Where(rule => rule.CurrentState.IsActive && rule.NeedsHedging()).ToList();

            if (!activeRules.Any())
            {
                return false;
            }

            if (activeRules.Any(rule => rule.HedgeStrategyType == HedgeStrategyType.Return))
            {
                return false;
            }

            return true;
        }
    }
}