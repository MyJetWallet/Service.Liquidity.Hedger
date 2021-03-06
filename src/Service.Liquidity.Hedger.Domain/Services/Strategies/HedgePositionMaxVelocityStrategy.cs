using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using Service.IndexPrices.Client;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
using Service.Liquidity.TradingPortfolio.Domain.Models;
using IHedgeStrategy = Service.Liquidity.Hedger.Domain.Interfaces.IHedgeStrategy;

namespace Service.Liquidity.Hedger.Domain.Services.Strategies
{
    public class HedgePositionMaxVelocityStrategy : IHedgeStrategy
    {
        public Dictionary<string, string> ParamValuesByName { get; set; } = new ();
        
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

        public HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, MonitoringRule rule)
        {
            var instruction = new HedgeInstruction();
                
            var selectedAssets = rule.Checks
                .Where(ch => ch.CurrentState.IsActive)
                .SelectMany(ch => ch.AssetSymbols)
                .ToHashSet();
            var selectedPositionAssets = portfolio.Assets
                .Select(a => a.Value)
                .Where(a => a.GetNegativeNetInUsd() != 0 && selectedAssets.Contains(a.Symbol))
                .OrderBy(a => a.DailyVelocityRiskInUsd)
                .ToList();
            var collateralAssets = portfolio.Assets
                .Select(a => a.Value)
                .Where(a => a.GetPositiveNetInUsd() != 0)
                .OrderBy(a => a.DailyVelocityRiskInUsd)
                .Select(a => new HedgePairAsset
                {
                    Weight = a.DailyVelocityRiskInUsd * -1,
                    Symbol = a.Symbol,
                    AvailableVolume = a.NetBalance
                })
                .ToList();


            var selectedAsset = selectedPositionAssets.FirstOrDefault();
            
            if (selectedAsset?.Symbol == null)
            {
                return instruction;
            }
            
            var targetVolumeInUsd = Math.Abs(selectedPositionAssets.Sum(a => a.NetBalanceInUsd)) *
                                    (HedgePercent / 100);
            
            if (selectedAssets.Count == 1)
            {
                instruction.TargetVolume = Math.Abs(selectedAsset.NetBalance * (HedgePercent / 100));
            }
            else
            {
                var price = selectedAsset.NetBalanceInUsd / selectedAsset.NetBalance;
                instruction.TargetVolume = Math.Abs(targetVolumeInUsd / price);
            }

            instruction.TargetAssetSymbol = selectedAsset.Symbol;
            instruction.PairAssets = collateralAssets;
            instruction.Date = DateTime.UtcNow;
            instruction.MonitoringRuleId = rule.Id;
            instruction.Weight = targetVolumeInUsd;
            instruction.TargetSide = OrderSide.Buy;

            return instruction;
        }
    }
}