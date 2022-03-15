using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services.Strategies
{
    public class ClosePositionMaxVelocityHedgeStrategy : IHedgeStrategy
    {
        private readonly ILogger<IHedgeStrategy> _logger;
        public HedgeStrategyType Type { get; set; } = HedgeStrategyType.ClosePositionMaxVelocity;

        public ClosePositionMaxVelocityHedgeStrategy(
            ILogger<IHedgeStrategy> logger)
        {
            _logger = logger;
        }

        public HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, IEnumerable<PortfolioCheck> checks,
            HedgeStrategyParams strategyParams)
        {
            var selectedAssets = checks
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
                .Select(a => new HedgeQuoteAssets
                {
                    Weight = a.DailyVelocityRiskInUsd,
                    Symbol = a.Symbol,
                })
                .ToList();

            var instruction = new HedgeInstruction
            {
                BaseAssetSymbol = selectedPositionAssets.FirstOrDefault()?.Symbol,
                QuoteAssets = collateralAssets,
                TargetVolume = Math.Abs(selectedPositionAssets.Sum(a => a.NetBalance)) *
                               (strategyParams.AmountPercent / 100)
            };

            _logger.LogInformation(
                "HedgeInstruction: {@instruction} {@selectedPositionAssets} {@collateralAssets}", instruction,
                selectedPositionAssets, collateralAssets);

            return instruction;
        }
    }
}