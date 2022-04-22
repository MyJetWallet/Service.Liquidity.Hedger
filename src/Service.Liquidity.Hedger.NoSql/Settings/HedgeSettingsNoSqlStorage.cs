using System.Collections.Generic;
using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql.Settings;

public class HedgeSettingsNoSqlStorage : IHedgeSettingsStorage
{
    private readonly IMyNoSqlServerDataWriter<HedgeSettingsNoSql> _dataWriter;

    public HedgeSettingsNoSqlStorage(
        IMyNoSqlServerDataWriter<HedgeSettingsNoSql> dataWriter
    )
    {
        _dataWriter = dataWriter;
    }

    public async Task AddOrUpdateAsync(HedgeSettings model)
    {
        var nosqlModel = HedgeSettingsNoSql.Create(model);
        await _dataWriter.InsertOrReplaceAsync(nosqlModel);
    }

    public async Task<HedgeSettings> GetAsync()
    {
        var settingsNoSql = await _dataWriter.GetAsync(
            HedgeSettingsNoSql.GeneratePartitionKey(),
            HedgeSettingsNoSql.GenerateRowKey());
        var settings = settingsNoSql?.Value ?? new HedgeSettings();
        settings.EnabledExchanges ??= new List<string>();
        settings.IndirectMarketTransitAssets ??= new List<string>();
        settings.DirectMarketLimitTradeSteps ??= new List<LimitTradeStep>();
        settings.IndirectMarketLimitTradeSteps ??= new List<LimitTradeStep>();
        
        return settings;
    }
}