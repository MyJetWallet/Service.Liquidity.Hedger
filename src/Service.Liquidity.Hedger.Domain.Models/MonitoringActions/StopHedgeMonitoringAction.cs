using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Monitoring.Domain.Models.Actions;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class StopHedgeMonitoringAction : IMonitoringAction
    {
        [DataMember(Order = 1)] public string TypeName { get; set; } = nameof(StopHedgeMonitoringAction);
        [DataMember(Order = 2)] public Dictionary<string, string> ParamValuesByName { get; set; }
        [DataMember(Order = 3)] public ICollection<MonitoringActionParamInfo> ParamInfos { get; set; }
    }
}