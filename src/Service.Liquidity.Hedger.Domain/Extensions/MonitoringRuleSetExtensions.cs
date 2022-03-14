using System.Linq;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;

namespace Service.Liquidity.Hedger.Domain.Extensions
{
    public static class MonitoringRuleSetExtensions
    {
        public static bool NeedsHedging(this MonitoringRuleSet ruleSet, out string message)
        {
            message = "";

            if (ruleSet.Rules == null || !ruleSet.Rules.Any())
            {
                message += "No rules;";
                return false;
            }

            message += "Has rules;";

            var activeRules = ruleSet.Rules.Where(rule => rule.CurrentState.IsActive).ToArray();

            if (!activeRules.Any())
            {
                message += "No active rules;";
                return false;
            }

            message += "Has active rules;";

            var hedgingRules = activeRules.Where(rule => rule.NeedsHedging(out _)).ToArray();

            if (!hedgingRules.Any())
            {
                message += "No hedging rules;";
                return false;
            }

            message += "Has hedging rules";

            if (hedgingRules.Any(rule => rule.HedgeStrategyType == HedgeStrategyType.Return))
            {
                message += "Contains Return rule;";
                return false;
            }

            message += "Don't has return rule;";

            return true;
        }
    }
}