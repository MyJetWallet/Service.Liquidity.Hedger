using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using MyJetWallet.Sdk.ServiceBus;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Monitoring.Domain.Models;

namespace Service.Liquidity.Hedger.Jobs
{
    public class HedgeJob : IStartable
    {
        private readonly ILogger<HedgeJob> _logger;
        private readonly IHedgeService _hedgeService;
        private readonly IServiceBusPublisher<HedgeOperation> _publisher;
        private readonly IHedgeInstructionsStorage _hedgeInstructionsStorage;
        private readonly IPortfolioAnalyzer _portfolioAnalyzer;
        private readonly IHedgeSettingsStorage _hedgeSettingsStorage;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly MyTaskTimer _timer;

        public HedgeJob(
            ILogger<HedgeJob> logger,
            IHedgeService hedgeService,
            IServiceBusPublisher<HedgeOperation> publisher,
            IHedgeInstructionsStorage hedgeInstructionsStorage,
            IPortfolioAnalyzer portfolioAnalyzer,
            IHedgeSettingsStorage hedgeSettingsStorage
        )
        {
            _logger = logger;
            _hedgeService = hedgeService;
            _publisher = publisher;
            _hedgeInstructionsStorage = hedgeInstructionsStorage;
            _portfolioAnalyzer = portfolioAnalyzer;
            _hedgeSettingsStorage = hedgeSettingsStorage;
            _timer = new MyTaskTimer(nameof(HedgeJob),
                TimeSpan.FromMilliseconds(500),
                logger,
                DoAsync).DisableTelemetry();
        }

        public void Start()
        {
            _timer.Start();
        }

        private async Task DoAsync()
        {
            var started = false;
            try
            {
                if (_semaphore.CurrentCount == 0)
                {
                    return;
                }

                await _semaphore.WaitAsync();
                started = true;
                _logger.LogInformation("{@Message} started", nameof(HedgeJob));
                
                var settings = await _hedgeSettingsStorage.GetAsync();

                if (!settings.EnabledExchanges?.Any() ?? true)
                {
                    _logger.LogWarning("Can't Hedge. No enabled exchanges");
                    return;
                }

                var instructions = (await _hedgeInstructionsStorage.GetAsync())?.ToList() ??
                                   new List<HedgeInstruction>();

                if (instructions.Count == 0 ||
                    instructions.Any(i => i.Status == HedgeInstructionStatus.InProgress))
                {
                    return;
                }

                var candidateInstructions = settings.ConfirmRequired
                    ? instructions.Where(i => i.Status == HedgeInstructionStatus.Confirmed)
                    : instructions.Where(i => i.Status == HedgeInstructionStatus.Pending ||
                                              i.Status == HedgeInstructionStatus.Confirmed);
                var hedgeInstruction = _portfolioAnalyzer.SelectPriorityInstruction(candidateInstructions);

                if (hedgeInstruction == null)
                {
                    return;
                }

                hedgeInstruction.Status = HedgeInstructionStatus.InProgress;
                await _hedgeInstructionsStorage.AddOrUpdateAsync(hedgeInstruction);
                var hedgeOperation = await _hedgeService.HedgeAsync(hedgeInstruction);
                await _hedgeInstructionsStorage.AddOrUpdateAsync(new List<HedgeInstruction>());

                if (hedgeOperation.HedgeTrades.Any())
                {
                    await _publisher.PublishAsync(hedgeOperation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to do {@Message}. {@ExMessage}", nameof(HedgeJob),
                    ex.Message);
            }
            finally
            {
                if (started)
                {
                    _logger.LogInformation("{@Message} ended", nameof(HedgeJob));
                    _semaphore.Release();
                }
            }
        }
    }
}