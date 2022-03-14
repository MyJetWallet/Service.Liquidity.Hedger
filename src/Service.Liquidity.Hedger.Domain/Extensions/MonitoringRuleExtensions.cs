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
                message += "Is not active;";
                return false;
            }

            message += "Is active;";
            
            if (rule.HedgeStrategyType == HedgeStrategyType.None)
            {
                message += "Has none strategy;";
                return false;
            }

            message += $"Has {rule.HedgeStrategyType.ToString()} strategy;";
            
            if (rule.HedgeStrategyParams == null || rule.HedgeStrategyParams.AmountPercent < 0)
            {
                message += "Doesn't Has hedge percent amount;";
                return false;
            }

            message += "Has hedge percent amount;";

            return true;
        }
    }
}