using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Grpc.HedgeSettings;
using Service.Liquidity.Hedger.Grpc.HedgeSettings.Models;

namespace Service.Liquidity.Hedger.Services
{
    public class HedgeSettingsService : IHedgeSettingsService
    {
        private readonly ILogger<HedgeSettingsService> _logger;
        private readonly IHedgeSettingsStorage _hedgeSettingsStorage;

        public HedgeSettingsService(
            ILogger<HedgeSettingsService> logger,
            IHedgeSettingsStorage hedgeSettingsStorage
        )
        {
            _logger = logger;
            _hedgeSettingsStorage = hedgeSettingsStorage;
        }

        public async Task<GetHedgeSettingsResponse> GetAsync(GetHedgeSettingRequest request)
        {
            try
            {
                var settings = await _hedgeSettingsStorage.GetAsync();

                return new GetHedgeSettingsResponse
                {
                    Item = settings
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to GetSettings. Request: {@request}", request);
                return new GetHedgeSettingsResponse
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<AddOrUpdateHedgeSettingResponse> AddOrUpdateAsync(AddOrUpdateHedgeSettingsRequest request)
        {
            try
            {
                await _hedgeSettingsStorage.AddOrUpdateAsync(request.Item);

                return new AddOrUpdateHedgeSettingResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to GetSettings. Request: {@request}", request);
                return new AddOrUpdateHedgeSettingResponse
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}