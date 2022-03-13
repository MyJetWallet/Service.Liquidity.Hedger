﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Hedger.Domain.Extensions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.Monitoring.Domain.Models.RuleSets;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services;

public class PortfolioAnalyzer : IPortfolioAnalyzer
{
    private readonly ILogger<PortfolioAnalyzer> _logger;
    private readonly IHedgeStrategiesFactory _hedgeStrategiesFactory;
    private readonly IHedgeOperationsStorage _hedgeOperationsStorage;

    public PortfolioAnalyzer(
        ILogger<PortfolioAnalyzer> logger,
        IHedgeStrategiesFactory hedgeStrategiesFactory,
        IHedgeOperationsStorage hedgeOperationsStorage
    )
    {
        _logger = logger;
        _hedgeStrategiesFactory = hedgeStrategiesFactory;
        _hedgeOperationsStorage = hedgeOperationsStorage;
    }

    public async Task<HedgeInstruction> CalculateHedgeInstructionAsync(Portfolio portfolio,
        ICollection<MonitoringRuleSet> ruleSets, ICollection<PortfolioCheck> checks)
    {
        var lastOperation = await _hedgeOperationsStorage.GetLastAsync();

        if (lastOperation != null &&
            portfolio.HedgeOperationId != null &&
            portfolio.HedgeOperationId < lastOperation.Id)
        {
            return null;
        }

        var hedgeInstruction = CalculateHedgeInstruction(portfolio, ruleSets, checks);

        if (hedgeInstruction == null)
        {
            _logger.LogWarning("No rule for hedging");
            return null;
        }

        return hedgeInstruction;
    }

    private HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, ICollection<MonitoringRuleSet> ruleSets,
        ICollection<PortfolioCheck> checks)
    {
        var hedgeInstructions = new List<HedgeInstruction>();

        foreach (var ruleSet in ruleSets ?? Array.Empty<MonitoringRuleSet>())
        {
            var isHedgeRuleSet = ruleSet.NeedsHedging(out var ruleSetMessage);

            _logger.LogInformation("RuleSet {@ruleSet} analyze message: {@message}", ruleSet, ruleSetMessage);

            if (!isHedgeRuleSet)
            {
                foreach (var rule in ruleSet.Rules ?? Array.Empty<MonitoringRule>())
                {
                    var isHedgeRule = rule.NeedsHedging(out var ruleMessage);

                    _logger.LogInformation("Rule {@ruleSet} analyze message: {@message}", ruleSet, ruleMessage);

                    if (isHedgeRule)
                    {
                        var ruleChecks = checks.Where(ch => rule.CheckIds.Contains(ch.Id));
                        var strategy = _hedgeStrategiesFactory.Get(rule.HedgeStrategyType);
                        var instruction =
                            strategy.CalculateHedgeInstruction(portfolio, ruleChecks, rule.HedgeStrategyParams);

                        if (instruction.Validate(out _))
                        {
                            _logger.LogInformation("Calculated hedge instruction {@instruction} for rule {@rule}",
                                instruction, rule);
                            hedgeInstructions.Add(instruction);
                        }
                    }
                }
            }
        }

        var highestPriorityInstruction = hedgeInstructions.MaxBy(instruction => instruction.BuyVolume);

        _logger.LogInformation("Highest priority hedge instruction: {@instruction}", highestPriorityInstruction);

        return highestPriorityInstruction;
    }
}