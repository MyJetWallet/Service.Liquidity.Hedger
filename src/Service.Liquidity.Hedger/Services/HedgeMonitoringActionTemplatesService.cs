using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.Extensions.Logging;
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

        public HedgeMonitoringActionTemplatesService(
            ILogger<HedgeMonitoringActionTemplatesService> logger
        )
        {
            _logger = logger;
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
                        await GetStopHedgeActionTemplateAsync(request.Action)
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
                    template.PossibleValues = Enum.GetValues<HedgeStrategyType>()
                        .Select(t =>
                        (
                            Value: ((int) t).ToString(),
                            DisplayValueValue: t.Humanize()
                        )).ToList();
                }

                if (paramInfo.Name == nameof(MakeHedgeMonitoringAction.HedgePercent))
                {
                    template.Validators = new List<IActionParamValidator>
                    {
                        new RangeValueActionParamValidator(1, 100),
                    };
                }

                if (monitoringAction?.ParamValuesByName != null &&
                    monitoringAction.ParamValuesByName.TryGetValue(paramInfo.Name, out var value))
                {
                    template.Value = value;
                    template.DisplayValue = template.PossibleValues?
                        .FirstOrDefault(v => v.Value == value).DisplayValue ?? value;
                }

                paramTemplates.Add(template);
            }

            return Task.FromResult(new MonitoringActionTemplate
            {
                Action = action,
                ParamTemplates = paramTemplates
            });
        }

        private Task<MonitoringActionTemplate> GetStopHedgeActionTemplateAsync(
            IMonitoringAction monitoringAction = null)
        {
            return Task.FromResult(new MonitoringActionTemplate
            {
                Action = new StopHedgeMonitoringAction(),
                ParamTemplates = new List<MonitoringActionParamTemplate>()
            });
        }
    }
}