using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets.Actions;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class MakeHedgeMonitoringAction : IMonitoringAction
    {
        [DataMember(Order = 1)] public string TypeName { get; set; } = nameof(MakeHedgeMonitoringAction);

        [DataMember(Order = 2)]
        public Dictionary<string, string> ParamValuesByName { get; set; } = new Dictionary<string, string>();

        [DataMember(Order = 3)]
        public ICollection<MonitoringActionParamInfo> ParamInfos { get; set; } =
            new List<MonitoringActionParamInfo>
            {
                new MonitoringActionParamInfo(nameof(HedgeStrategyType), MonitoringActionParamType.Int),
                new MonitoringActionParamInfo(nameof(HedgePercent), MonitoringActionParamType.Decimal),
            };

        [DataMember(Order = 4)] public int HedgeStrategyType { get; set; }
        [DataMember(Order = 5)] public decimal HedgePercent { get; set; }
    }
}