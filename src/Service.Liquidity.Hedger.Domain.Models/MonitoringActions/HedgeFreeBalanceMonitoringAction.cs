using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Service.Liquidity.Monitoring.Domain.Models.Actions;

namespace Service.Liquidity.Hedger.Domain.Models;

public class HedgeFreeBalanceMonitoringAction : IMonitoringAction
{
    [DataMember(Order = 1)] public string TypeName { get; set; } = nameof(HedgeFreeBalanceMonitoringAction);

    [DataMember(Order = 2)]
    public Dictionary<string, string> ParamValuesByName { get; set; } = new()
    {
        {nameof(HedgeStrategyType), ((int) HedgeStrategyType.HedgeFreeBalance).ToString()},
        {nameof(HedgePercent), "100"},
        {nameof(ReservedVolume), "0"},
        {nameof(PairAssetSymbol), "USD"},
    };

    [DataMember(Order = 3)]
    public ICollection<MonitoringActionParamInfo> ParamInfos { get; set; } =
        new List<MonitoringActionParamInfo>
        {
            new(nameof(HedgeStrategyType), MonitoringActionParamType.Int),
            new(nameof(HedgePercent), MonitoringActionParamType.Decimal),
            new(nameof(ReservedVolume), MonitoringActionParamType.Decimal),
            new(nameof(PairAssetSymbol), MonitoringActionParamType.String),
        };

    [DataMember(Order = 4)]
    public HedgeStrategyType HedgeStrategyType => HedgeStrategyType.HedgeFreeBalance;

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

    [DataMember(Order = 6)]
    public decimal ReservedVolume
    {
        get
        {
            var strValue = ParamValuesByName[nameof(ReservedVolume)];

            return decimal.Parse(strValue);
        }
        set => ParamValuesByName[nameof(ReservedVolume)] = value.ToString(CultureInfo.InvariantCulture);
    }


    [DataMember(Order = 7)]
    public string PairAssetSymbol
    {
        get => ParamValuesByName[nameof(PairAssetSymbol)];
        set => ParamValuesByName[nameof(PairAssetSymbol)] = value;
    }
}