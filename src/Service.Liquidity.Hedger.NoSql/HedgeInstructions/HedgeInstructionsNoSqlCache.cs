using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql.HedgeInstructions;

public class HedgeInstructionsNoSqlCache : IHedgeInstructionsCache, IStartable
{
    private readonly IMyNoSqlServerDataReader<HedgeInstructionNoSql> _reader;
    private List<HedgeInstruction> _data = new();

    public HedgeInstructionsNoSqlCache(
        IMyNoSqlServerDataReader<HedgeInstructionNoSql> reader
    )
    {
        _reader = reader;
    }

    public void Start()
    {
        _reader.SubscribeToUpdateEvents(models =>
        {
            lock (_data)
            {
                LoadData();
            }
        }, models =>
        {
            lock (_data)
            {
                LoadData();
            }
        });
    }

    public Task<IEnumerable<HedgeInstruction>> GetAsync()
    {
        if (!_data.Any())
        {
            LoadData();
        }

        return Task.FromResult<IEnumerable<HedgeInstruction>>(_data);
    }

    private void LoadData()
    {
        _data = _reader.Get()?.Select(m => m.Value).ToList() ?? new List<HedgeInstruction>();
    }
}