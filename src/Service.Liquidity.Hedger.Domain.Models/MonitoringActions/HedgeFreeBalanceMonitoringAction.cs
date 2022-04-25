using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Service.Liquidity.Monitoring.Domain.Models.Actions;

namespace Service.Liquidity.Hedger.Domain.Models;

public class HedgeFreeBalanceMonitoringAction : IMonitoringAction
{
    [DataMember(Order = 1)] public string TypeName { get; set; }

    [DataMember(Order = 2)]
    public Dictionary<string, string> ParamValuesByName { get; set; } = new()
    {
        {nameof(HedgeStrategyType), nameof(HedgeStrategyType.HedgeFreeBalance)},
        {nameof(HedgePercent), "100"}
    };

    [DataMember(Order = 3)]
    public ICollection<MonitoringActionParamInfo> ParamInfos { get; set; } =
        new List<MonitoringActionParamInfo>
        {
            new(nameof(HedgeStrategyType), MonitoringActionParamType.Int),
            new(nameof(HedgePercent), MonitoringActionParamType.Decimal),
        };

    [DataMember(Order = 4)]
    public HedgeStrategyType HedgeStrategyType
    {
        get
        {
            var strValue = ParamValuesByName[nameof(HedgeStrategyType)];
            var intValue = int.Parse(strValue);
            var enumValue = (HedgeStrategyType) intValue;

            if (enumValue != HedgeStrategyType.HedgeFreeBalance)
            {
                throw new(
                    $"Invalid strategy type for {nameof(HedgePositionMaxVelocityMonitoringAction)}. Value must be {nameof(HedgeStrategyType.HedgeFreeBalance)}");
            }

            return enumValue;
        }
    }

    [DataMember(Order = 5)]
    public decimal HedgePercent
    {
        get
        {
            var strValue = ParamValuesByName[nameof(HedgePercent)];

            return decimal.Parse(strValue);
        }
        set => ParamValuesByName[nameof(HedgePercent)] = value.ToString(CultureInfo.InvariantCulture);
    }
}