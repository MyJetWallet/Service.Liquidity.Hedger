using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services;

public class PortfolioAnalyzer : IPortfolioAnalyzer
{
    private readonly ILogger<PortfolioAnalyzer> _logger;
    private readonly IHedgeStrategiesFactory _hedgeStrategiesFactory;
    private readonly IHedgeOperationsStorage _hedgeOperationsStorage;

    private static HashSet<string> _hedgeActionTypeNames = new()
    {
        nameof(MakeHedgeMonitoringAction),
        nameof(HedgeFreeBalanceMonitoringAction),
        nameof(HedgePositionMaxVelocityMonitoringAction),
    };

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

    public ICollection<MonitoringRule> SelectHedgeRules(ICollection<MonitoringRule> rules)
    {
        var hedgeRules = new List<MonitoringRule>();

        foreach (var rule in rules ?? Array.Empty<MonitoringRule>())
        {
            var isHedgeRule = NeedsHedging(rule, out var ruleMessage);

            if (!isHedgeRule)
            {
                continue;
            }

            hedgeRules.Add(rule);
        }

        return hedgeRules;
    }

    public ICollection<HedgeInstruction> CalculateHedgeInstructions(Portfolio portfolio,
        ICollection<MonitoringRule> rules)
    {
        var hedgeInstructions = new List<HedgeInstruction>();

        foreach (var rule in rules ?? Array.Empty<MonitoringRule>())
        {
            var hedgeActions = rule.ActionsByTypeName.Values
                .Where(action => _hedgeActionTypeNames.Contains(action.TypeName));
            var hedgeAction = new MakeHedgeMonitoringAction();
            var hedgeStrategies = hedgeActions.Select(action =>
            {
                action.CopyTo(hedgeAction);
                return _hedgeStrategiesFactory.Get(hedgeAction.HedgeStrategyType, hedgeAction.ParamValuesByName);
            });

            foreach (var strategy in hedgeStrategies)
            {
                var instruction = strategy.CalculateHedgeInstruction(portfolio, rule);

                if (instruction.Validate(out var message))
                {
                    hedgeInstructions.Add(instruction);
                }
                else
                {
                    _logger.LogWarning("HedgeInstruction is skipped: {@Instruction} {@Message}",
                        instruction, message);
                }
            }
        }

        return hedgeInstructions;
    }

    public HedgeInstruction SelectPriorityInstruction(IEnumerable<HedgeInstruction> instructions)
    {
        var hedgeInstruction = instructions.MaxBy(instruction => instruction.Weight);

        return hedgeInstruction;
    }

    private bool NeedsHedging(MonitoringRule rule, out string message)
    {
        message = "";

        if (rule.ActionsByTypeName == null)
        {
            message += "Doesn't has any action";
            return false;
        }

        if (_hedgeActionTypeNames.All(type => !rule.ActionsByTypeName.TryGetValue(type, out _)))
        {
            message += "Doesn't has hedge action;";
            return false;
        }

        if (!rule.CurrentState.IsActive)
        {
            message += "Is not active;";
            return false;
        }

        message += "Is active;";

        return true;
    }
}