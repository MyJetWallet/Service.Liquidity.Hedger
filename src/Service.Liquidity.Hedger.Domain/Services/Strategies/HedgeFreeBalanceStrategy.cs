using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyJetWallet.Domain.Orders;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services.Strategies;

public class HedgeFreeBalanceStrategy : IHedgeStrategy
{
    public Dictionary<string, string> ParamValuesByName { get; set; }

    public decimal HedgePercent
    {
        get
        {
            ParamValuesByName ??= new Dictionary<string, string>();
            var strValue = ParamValuesByName[nameof(HedgePercent)];

            return decimal.Parse(strValue);
        }
        set
        {
            ParamValuesByName ??= new Dictionary<string, string>();
            ParamValuesByName[nameof(HedgePercent)] = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public string PairAssetSymbol
    {
        get
        {
            ParamValuesByName ??= new Dictionary<string, string>();
            return ParamValuesByName[nameof(PairAssetSymbol)];
        }
        set
        {
            ParamValuesByName ??= new Dictionary<string, string>();
            ParamValuesByName[nameof(PairAssetSymbol)] = value;
        }
    }

    public decimal ReservedVolume
    {
        get
        {
            ParamValuesByName ??= new Dictionary<string, string>();
            var strValue = ParamValuesByName[nameof(ReservedVolume)];

            return decimal.Parse(strValue);
        }
        set
        {
            ParamValuesByName ??= new Dictionary<string, string>();
            ParamValuesByName[nameof(ReservedVolume)] = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, MonitoringRule rule)
    {
        var instruction = new HedgeInstruction();

        var selectedAssets = rule.Checks
            .Where(ch => ch.CurrentState.IsActive)
            .SelectMany(ch => ch.AssetSymbols)
            .ToHashSet();
        var selectedCollateralAssets = portfolio.Assets
            .Select(a => a.Value)
            .Where(a => a.GetPositiveNetInUsd() != 0 && selectedAssets.Contains(a.Symbol))
            .OrderBy(a => a.DailyVelocityRiskInUsd)
            .ToList();
        var selectedPairAsset = portfolio.Assets
            .FirstOrDefault(a => a.Value.Symbol == PairAssetSymbol)
            .Value;

        if (selectedPairAsset == null)
        {
            return instruction;
        }

        var selectedAsset = selectedCollateralAssets.MinBy(a => a.DailyVelocityRiskInUsd);

        if (selectedAsset?.Symbol == null)
        {
            return instruction;
        }

        var price = selectedAsset.NetBalanceInUsd / selectedAsset.NetBalance;
        var targetVolumeInUsd = Math.Abs(selectedCollateralAssets.Sum(a => a.NetBalanceInUsd)) *
            (HedgePercent / 100) - ReservedVolume * price;
        instruction.TargetVolume = Math.Abs(targetVolumeInUsd / price) - ReservedVolume;
        instruction.TargetAssetSymbol = selectedAsset.Symbol;
        instruction.PairAssets = new List<HedgePairAsset>
        {
            new()
            {
                Weight = selectedPairAsset.DailyVelocityRiskInUsd * -1,
                Symbol = selectedPairAsset.Symbol,
                AvailableVolume = selectedPairAsset.NetBalance
            }
        };
        instruction.Date = DateTime.UtcNow;
        instruction.MonitoringRuleId = rule.Id;
        instruction.Weight = targetVolumeInUsd;
        instruction.TargetSide = OrderSide.Sell;

        return instruction;
    }
}