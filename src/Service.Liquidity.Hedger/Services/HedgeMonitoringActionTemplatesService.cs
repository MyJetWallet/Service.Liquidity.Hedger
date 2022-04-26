using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using Service.AssetsDictionary.MyNoSql;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models.Actions;
using Service.Liquidity.Monitoring.Domain.Models.Actions.Templates;
using Service.Liquidity.Monitoring.Domain.Models.Actions.Validators;
using Service.Liquidity.Monitoring.Grpc;
using Service.Liquidity.Monitoring.Grpc.Models.ActionTemplates;

namespace Service.Liquidity.Hedger.Services
{
    public class HedgeMonitoringActionTemplatesService : IMonitoringActionTemplatesService
    {
        private readonly ILogger<HedgeMonitoringActionTemplatesService> _logger;
        private readonly IMyNoSqlServerDataReader<AssetNoSqlEntity> _assetsNoSqlReader;

        public HedgeMonitoringActionTemplatesService(
            ILogger<HedgeMonitoringActionTemplatesService> logger,
            IMyNoSqlServerDataReader<AssetNoSqlEntity> assetsNoSqlReader
        )
        {
            _logger = logger;
            _assetsNoSqlReader = assetsNoSqlReader;
        }

        public async Task<GetMonitoringActionTemplateListResponse> GetListAsync(
            GetMonitoringActionTemplateListRequest request)
        {
            try
            {
                return new GetMonitoringActionTemplateListResponse
                {
                    Templates = new List<MonitoringActionTemplate>
                    {
                        await GetMakeHedgeActionTemplateAsync(request.Action),
                        await GetStopHedgeActionTemplateAsync(request.Action),
                        await GetHedgePositionMaxVelocityActionTemplateAsync(request.Action)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get ActionTemplateList. {@Request}", request);
                return new GetMonitoringActionTemplateListResponse
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetMonitoringActionTemplateResponse> GetAsync(GetMonitoringActionTemplateRequest request)
        {
            try
            {
                var actionTemplate = request.Action.TypeName switch
                {
                    nameof(StopHedgeMonitoringAction) => await GetStopHedgeActionTemplateAsync(request.Action),
                    nameof(MakeHedgeMonitoringAction) => await GetMakeHedgeActionTemplateAsync(request.Action),
                    nameof(HedgePositionMaxVelocityMonitoringAction) =>
                        await GetHedgePositionMaxVelocityActionTemplateAsync(request.Action),
                    _ => throw new NotSupportedException($"{nameof(request.Action.TypeName)}")
                };

                return new GetMonitoringActionTemplateResponse
                {
                    Template = actionTemplate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get ActionTemplate. {@Request}", request);
                return new GetMonitoringActionTemplateResponse
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        private Task<MonitoringActionTemplate> GetHedgePositionMaxVelocityActionTemplateAsync(
            IMonitoringAction monitoringAction = null)
        {
            var action = new HedgePositionMaxVelocityMonitoringAction();
            var paramTemplates = new List<MonitoringActionParamTemplate>();

            foreach (var paramInfo in action.ParamInfos)
            {
                var template = new MonitoringActionParamTemplate
                {
                    Name = paramInfo.Name,
                    Type = paramInfo.Type,
                    Value = "",
                    DisplayValue = "",
                    DisplayName = paramInfo.Name.Humanize(),
                    PossibleValues = new List<(string Value, string DisplayValue)>()
                };

                if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgeStrategyType))
                {
                    template = GetHedgeStrategyTypeParamTemplate(paramInfo);
                }

                if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgePercent))
                {
                    template = GetHedgePercentParamTemplate(paramInfo);
                }

                TryInitParamTemplateValue(monitoringAction, paramInfo, template);

                paramTemplates.Add(template);
            }

            return Task.FromResult(new MonitoringActionTemplate
            {
                Action = new MonitoringAction(action),
                ParamTemplates = paramTemplates
            });
        }

        private Task<MonitoringActionTemplate> GetHedgeFreeBalanceActionTemplateAsync(
            IMonitoringAction monitoringAction = null)
        {
            var action = new HedgeFreeBalanceMonitoringAction();
            var paramTemplates = new List<MonitoringActionParamTemplate>();

            foreach (var paramInfo in action.ParamInfos)
            {
                var template = new MonitoringActionParamTemplate
                {
                    Name = paramInfo.Name,
                    Type = paramInfo.Type,
                    Value = "",
                    DisplayValue = "",
                    DisplayName = paramInfo.Name.Humanize(),
                    PossibleValues = new List<(string Value, string DisplayValue)>()
                };

                if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgeStrategyType))
                {
                    template = GetHedgeStrategyTypeParamTemplate(paramInfo);
                }

                if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgePercent))
                {
                    template = GetHedgePercentParamTemplate(paramInfo);
                }

                if (paramInfo.Name == nameof(HedgeFreeBalanceMonitoringAction.PairAssetSymbol))
                {
                    var assets = _assetsNoSqlReader.Get();
                    template.PossibleValues = assets
                        .Select(a =>
                        (
                            Value: a.Symbol,
                            DisplayValueValue: a.Symbol
                        )).ToList();
                }

                TryInitParamTemplateValue(monitoringAction, paramInfo, template);

                paramTemplates.Add(template);
            }

            return Task.FromResult(new MonitoringActionTemplate
            {
                Action = new MonitoringAction(action),
                ParamTemplates = paramTemplates
            });
        }

        private Task<MonitoringActionTemplate> GetMakeHedgeActionTemplateAsync(
            IMonitoringAction monitoringAction = null)
        {
            var action = new MakeHedgeMonitoringAction();
            var paramTemplates = new List<MonitoringActionParamTemplate>();

            foreach (var paramInfo in action.ParamInfos)
            {
                var template = new MonitoringActionParamTemplate
                {
                    Name = paramInfo.Name,
                    Type = paramInfo.Type,
                    Value = "",
                    DisplayValue = "",
                    DisplayName = paramInfo.Name.Humanize(),
                    PossibleValues = new List<(string Value, string DisplayValue)>()
                };

                if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgeStrategyType))
                {
                    template = GetHedgeStrategyTypeParamTemplate(paramInfo);
                }

                if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgePercent))
                {
                    template = GetHedgePercentParamTemplate(paramInfo);
                }

                TryInitParamTemplateValue(monitoringAction, paramInfo, template);

                paramTemplates.Add(template);
            }

            return Task.FromResult(new MonitoringActionTemplate
            {
                Action = new MonitoringAction(action),
                ParamTemplates = paramTemplates
            });
        }

        private Task<MonitoringActionTemplate> GetStopHedgeActionTemplateAsync(
            IMonitoringAction monitoringAction = null)
        {
            return Task.FromResult(new MonitoringActionTemplate
            {
                Action = new MonitoringAction(new StopHedgeMonitoringAction()),
                ParamTemplates = new List<MonitoringActionParamTemplate>()
            });
        }

        private MonitoringActionParamTemplate GetHedgeStrategyTypeParamTemplate(MonitoringActionParamInfo paramInfo)
        {
            var template = new MonitoringActionParamTemplate
            {
                Name = paramInfo.Name,
                Type = paramInfo.Type,
                Value = "",
                DisplayValue = "",
                DisplayName = paramInfo.Name.Humanize(),
                PossibleValues = new List<(string Value, string DisplayValue)>()
            };

            if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgeStrategyType))
            {
                template.PossibleValues = Enum.GetValues<HedgeStrategyType>()
                    .Select(t =>
                    (
                        Value: ((int) t).ToString(),
                        DisplayValueValue: t.Humanize()
                    )).ToList();
            }

            return template;
        }

        private MonitoringActionParamTemplate GetHedgePercentParamTemplate(MonitoringActionParamInfo paramInfo)
        {
            var template = new MonitoringActionParamTemplate
            {
                Name = paramInfo.Name,
                Type = paramInfo.Type,
                Value = "",
                DisplayValue = "",
                DisplayName = paramInfo.Name.Humanize(),
                PossibleValues = new List<(string Value, string DisplayValue)>()
            };

            if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgePercent))
            {
                var rangeValidator = new RangeValueActionParamValidator(1, 100);
                template.Validators = new List<ActionParamValidator>
                {
                    new ActionParamValidator {ParamValuesByName = rangeValidator.ParamValuesByName, Type = rangeValidator.Type},
                };
            }

            return template;
        }

        private void TryInitParamTemplateValue(IMonitoringAction monitoringAction,
            MonitoringActionParamInfo paramInfo,
            MonitoringActionParamTemplate template)
        {
            if (monitoringAction?.ParamValuesByName != null &&
                monitoringAction.ParamValuesByName.TryGetValue(paramInfo.Name, out var value))
            {
                template.Value = value;
                template.DisplayValue = template.PossibleValues?
                    .FirstOrDefault(v => v.Value == value).DisplayValue ?? value;
            }
        }
    }
}