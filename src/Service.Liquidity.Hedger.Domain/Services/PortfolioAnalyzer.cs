using System;
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

        if (lastOperation != null && portfolio.HedgeOperationId == null)
        {
            _logger.LogWarning(
                "Can't CalculateHedgeInstruction. There is HedgeOperation but HedgeOperationId in Portfolio is empty {@hedgeOperation}",
                lastOperation);
            return null;
        }

        if (lastOperation != null &&
            portfolio.HedgeOperationId != null &&
            portfolio.HedgeOperationId == lastOperation.Id)
        {
            _logger.LogWarning(
                "Can't CalculateHedgeInstruction. HedgeOperationId in Portfolio doesn't equals. Portfolio.OperationId={@port} != OperationId={@operation}",
                portfolio.HedgeOperationId, lastOperation.Id);
            return null;
        }

        var hedgeInstruction = CalculateHedgeInstruction(portfolio, ruleSets, checks);

        return hedgeInstruction;
    }

    private HedgeInstruction CalculateHedgeInstruction(Portfolio portfolio, ICollection<MonitoringRuleSet> ruleSets,
        ICollection<PortfolioCheck> checks)
    {
        var hedgeInstructions = new List<HedgeInstruction>();

        foreach (var ruleSet in ruleSets ?? Array.Empty<MonitoringRuleSet>())
        {
            var isHedgeRuleSet = ruleSet.NeedsHedging(out var ruleSetMessage);

            if (!isHedgeRuleSet)
            {
                continue;
            }

            _logger.LogInformation("Found hedging RuleSet {@ruleSet}: {@message}", ruleSet, ruleSetMessage);

            foreach (var rule in ruleSet.Rules ?? Array.Empty<MonitoringRule>())
            {
                var isHedgeRule = rule.NeedsHedging(out var ruleMessage);

                if (!isHedgeRule)
                {
                    continue;
                }

                _logger.LogInformation("Found hedging Rule {@rule}: {@message}", rule, ruleMessage);

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

        if (!hedgeInstructions.Any())
        {
            return null;
        }

        var highestPriorityInstruction = hedgeInstructions.MaxBy(instruction => instruction.TargetVolume);

        _logger.LogInformation("Highest priority hedge instruction: {@instruction}", highestPriorityInstruction);

        return highestPriorityInstruction;
    }
}