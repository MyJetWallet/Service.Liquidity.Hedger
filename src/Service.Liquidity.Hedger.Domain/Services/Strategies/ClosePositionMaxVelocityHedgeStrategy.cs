using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Service.IndexPrices.Client;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services.Strategies
{
    public class ClosePositionMaxVelocityHedgeStrategy : IHedgeStrategy
    {
        public HedgeStrategyType Type { get; set; } = HedgeStrategyType.ClosePositionMaxVelocity;

        public HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, MonitoringRule rule, decimal hedgePercent)
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
                })
                .ToList();


            var selectedAsset = selectedPositionAssets.FirstOrDefault();
            
            if (selectedAsset?.Symbol == null)
            {
                return instruction;
            }
            
            var targetVolumeInUsd = Math.Abs(selectedPositionAssets.Sum(a => a.NetBalanceInUsd)) *
                                    (hedgePercent / 100);
            
            if (selectedAssets.Count == 1)
            {
                instruction.TargetVolume = Math.Abs(selectedAsset.NetBalance * (hedgePercent / 100));
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

            return instruction;
        }
    }
}