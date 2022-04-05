using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql.HedgeInstructions
{
    public class HedgeInstructionsNoSqlStorage : IHedgeInstructionsStorage
    {
        private readonly IMyNoSqlServerDataWriter<HedgeInstructionNoSql> _myNoSqlServerDataWriter;

        public HedgeInstructionsNoSqlStorage(
            IMyNoSqlServerDataWriter<HedgeInstructionNoSql> myNoSqlServerDataWriter
        )
        {
            _myNoSqlServerDataWriter = myNoSqlServerDataWriter;
        }

        public async Task AddOrUpdateAsync(HedgeInstruction model)
        {
            var nosqlModel = HedgeInstructionNoSql.Create(model);
            await _myNoSqlServerDataWriter.InsertOrReplaceAsync(nosqlModel);
        }

        public async Task<IEnumerable<HedgeInstruction>> GetAsync(HedgeInstructionStatus? status = null)
        {
            var models = (await _myNoSqlServerDataWriter.GetAsync())?
                .Select(m => m.Value) ?? new List<HedgeInstruction>();

            return status.HasValue
                ? models.Where(m => m.Status == status)
                : models;
        }

        public async Task<HedgeInstruction> GetAsync(string monitoringRuleId)
        {
            var model = await _myNoSqlServerDataWriter.GetAsync(
                HedgeInstructionNoSql.GeneratePartitionKey(),
                HedgeInstructionNoSql.GenerateRowKey(monitoringRuleId));

            return model?.Value;
        }

        public async Task DeleteAsync(string monitoringRuleId)
        {
            await _myNoSqlServerDataWriter.DeleteAsync(HedgeInstructionNoSql.GeneratePartitionKey(),
                HedgeInstructionNoSql.GenerateRowKey(monitoringRuleId));
        }

        public async Task AddOrUpdateAsync(IEnumerable<HedgeInstruction> models)
        {
            var nosqlModels = models.Select(HedgeInstructionNoSql.Create);
            await _myNoSqlServerDataWriter.BulkInsertOrReplaceAsync(nosqlModels);
        }
    }
}