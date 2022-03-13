using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;

namespace Service.Liquidity.Hedger.Domain.Extensions
{
    public static class MonitoringRuleExtensions
    {
        public static bool NeedsHedging(this MonitoringRule rule, out string message)
        {
            message = "";

            if (!rule.CurrentState.IsActive)
            {
                message += "Is not active";
                return false;
            }
            
            if (rule.HedgeStrategyType == HedgeStrategyType.None)
            {
                message += "Has none strategy";
                return false;
            }

            return true;
        }
    }
}