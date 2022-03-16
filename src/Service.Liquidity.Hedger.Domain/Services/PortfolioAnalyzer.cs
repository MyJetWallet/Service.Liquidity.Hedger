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

    public async Task<bool> TimeToHedge(Portfolio portfolio)
    {
        var lastOperation = await _hedgeOperationsStorage.GetLastAsync();

        if (lastOperation != null && portfolio.HedgeOperationId == null)
        {
            _logger.LogWarning(
                "Can't Hedge. There is HedgeOperation but HedgeOperationId in Portfolio is empty {@lastOperation}",
                lastOperation);
            return false;
        }

        if (lastOperation != null &&
            portfolio.HedgeOperationId != null &&
            portfolio.HedgeOperationId != lastOperation.Id)
        {
            _logger.LogWarning(
                "Can't Hedge. HedgeOperationId in Portfolio doesn't equals. Portfolio.OperationId={@portfolioOperationId} != OperationId={@lastOperationId}",
                portfolio.HedgeOperationId, lastOperation.Id);
            return false;
        }

        return true;
    }

    public ICollection<MonitoringRule> SelectHedgeRules(ICollection<MonitoringRuleSet> ruleSets)
    {
        var rules = new List<MonitoringRule>();

        foreach (var ruleSet in ruleSets ?? Array.Empty<MonitoringRuleSet>())
        {
            var isHedgeRuleSet = ruleSet.NeedsHedging(out var ruleSetMessage);

            if (!isHedgeRuleSet)
            {
                continue;
            }

            _logger.LogInformation("Found hedging RuleSet {@ruleSet}: {@message}", ruleSet.Name, ruleSetMessage);

            foreach (var rule in ruleSet.Rules ?? Array.Empty<MonitoringRule>())
            {
                var isHedgeRule = rule.NeedsHedging(out var ruleMessage);

                if (!isHedgeRule)
                {
                    continue;
                }

                _logger.LogInformation("Found hedging Rule {@rule}: {@message}", rule.Name, ruleMessage);
                rules.Add(rule);
            }
        }

        
        
        return rules;
    }

    public ICollection<HedgeInstruction> CalculateHedgeInstructions(Portfolio portfolio,
        ICollection<MonitoringRule> rules, ICollection<PortfolioCheck> checks)
    {
        var hedgeInstructions = new List<HedgeInstruction>();

        foreach (var rule in rules ?? Array.Empty<MonitoringRule>())
        {
            var ruleChecks = checks.Where(ch => rule.CheckIds.Contains(ch.Id));
            var strategy = _hedgeStrategiesFactory.Get(rule.HedgeStrategyType);
            var instruction = strategy.CalculateHedgeInstruction(portfolio, ruleChecks, rule.HedgeStrategyParams);

            if (instruction.Validate(out var message))
            {
                hedgeInstructions.Add(instruction);
            }
            else
            {
                _logger.LogWarning("HedgeInstruction is skipped: {@instruction} {@message}",
                    instruction, message);
            }
        }

        return hedgeInstructions;
    }
    
    public HedgeInstruction SelectPriorityInstruction(IEnumerable<HedgeInstruction> instructions)
    {
        var hedgeInstruction = instructions.MaxBy(instruction => instruction.TargetVolume);

        _logger.LogInformation("SelectPriorityInstruction: {@instruction}", hedgeInstruction);

        return hedgeInstruction;
    }
}