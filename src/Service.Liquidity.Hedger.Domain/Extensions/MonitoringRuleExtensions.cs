using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;

namespace Service.Liquidity.Hedger.Domain.Extensions
{
    public static class MonitoringRuleExtensions
    {
        public static bool NeedsHedging(this MonitoringRule rule)
        {
            //if (rule.HedgeStrategyType == HedgeStrategyType.None)
            {
                return false;
            }

            return true;
        }
    }
}