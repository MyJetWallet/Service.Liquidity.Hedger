using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.ServiceBus;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Hedger.Grpc.HedgeInstructions;
using Service.Liquidity.Hedger.Grpc.HedgeInstructions.Models;

namespace Service.Liquidity.Hedger.Services
{
    public class HedgeInstructionsService : IHedgeInstructionsService
    {
        private readonly IHedgeInstructionsStorage _hedgeInstructionsStorage;
        private readonly ILogger<HedgeInstructionsService> _logger;
        private readonly IHedgeInstructionsCache _hedgeInstructionsCache;
        private readonly IServiceBusPublisher<ConfirmedHedgeInstruction> _publisher;

        public HedgeInstructionsService(
            IHedgeInstructionsStorage hedgeInstructionsStorage,
            ILogger<HedgeInstructionsService> logger,
            IHedgeInstructionsCache hedgeInstructionsCache,
            IServiceBusPublisher<ConfirmedHedgeInstruction> publisher
        )
        {
            _hedgeInstructionsStorage = hedgeInstructionsStorage;
            _logger = logger;
            _hedgeInstructionsCache = hedgeInstructionsCache;
            _publisher = publisher;
        }

        public async Task<GetHedgeInstructionListResponse> GetListAsync(GetHedgeInstructionListRequest request)
        {
            try
            {
                var items = (await _hedgeInstructionsCache.GetAsync())?.ToArray();

                return new GetHedgeInstructionListResponse
                {
                    Items = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to GetList of HedgeInstructions. {@Message}", ex.Message);
                return new GetHedgeInstructionListResponse
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<AddOrUpdateHedgeInstructionResponse> AddOrUpdateAsync(
            AddOrUpdateHedgeInstructionRequest request)
        {
            try
            {
                await _hedgeInstructionsStorage.AddOrUpdateAsync(request.Item);

                if (request.Item.Status == HedgeInstructionStatus.Confirmed)
                {
                    await _publisher.PublishAsync(new ConfirmedHedgeInstruction
                    {
                        HedgeInstruction = request.Item
                    });
                }

                return new AddOrUpdateHedgeInstructionResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to AddOrUpdate HedgeInstruction. {@Message}", ex.Message);
                return new AddOrUpdateHedgeInstructionResponse
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetHedgeInstructionResponse> GetAsync(GetHedgeInstructionRequest request)
        {
            try
            {
                var item = await _hedgeInstructionsStorage.GetAsync(request.Id);

                return new GetHedgeInstructionResponse
                {
                    Item = item
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to Get HedgeInstruction. {@Message}", ex.Message);
                return new GetHedgeInstructionResponse
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DeleteHedgeInstructionResponse> DeleteAsync(DeleteHedgeInstructionRequest request)
        {
            try
            {
                await _hedgeInstructionsStorage.DeleteAsync(request.Id);

                return new DeleteHedgeInstructionResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to Delete HedgeInstruction. {@Message}", ex.Message);
                return new DeleteHedgeInstructionResponse
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}