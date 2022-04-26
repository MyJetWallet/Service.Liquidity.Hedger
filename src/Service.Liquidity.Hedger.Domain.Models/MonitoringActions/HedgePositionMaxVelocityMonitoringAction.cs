using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Service.Liquidity.Monitoring.Domain.Models.Actions;

namespace Service.Liquidity.Hedger.Domain.Models;

[DataContract]
public class HedgePositionMaxVelocityMonitoringAction : IMonitoringAction
{
    [DataMember(Order = 1)] public string TypeName { get; set; } = nameof(HedgePositionMaxVelocityMonitoringAction);

    [DataMember(Order = 2)]
    public Dictionary<string, string> ParamValuesByName { get; set; } = new()
    {
        {nameof(HedgeStrategyType), ((int)HedgeStrategyType.HedgePositionMaxVelocity).ToString()},
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
    public HedgeStrategyType HedgeStrategyType => HedgeStrategyType.HedgePositionMaxVelocity;


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