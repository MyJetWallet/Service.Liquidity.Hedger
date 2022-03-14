using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql
{
    public class HedgeOperationsNoSqlStorage : IHedgeOperationsStorage
    {
        private readonly IMyNoSqlServerDataWriter<HedgeOperationNoSql> _myNoSqlServerDataWriter;
        private static HedgeOperation _cachedLastOperation;

        public HedgeOperationsNoSqlStorage(
            IMyNoSqlServerDataWriter<HedgeOperationNoSql> myNoSqlServerDataWriter
        )
        {
            _myNoSqlServerDataWriter = myNoSqlServerDataWriter;
        }

        public async Task<long> GetNextIdAsync()
        {
            if (_cachedLastOperation != null)
            {
                return _cachedLastOperation.Id += 1;
            }

            var model = await _myNoSqlServerDataWriter.GetAsync(
                HedgeOperationNoSql.GeneratePartitionKey(),
                HedgeOperationNoSql.LastRowKey);
            _cachedLastOperation = model?.Value ?? new HedgeOperation();

            return _cachedLastOperation.Id += 1;
        }

        public async Task AddOrUpdateLastAsync(HedgeOperation model)
        {
            var nosqlModel = HedgeOperationNoSql.CreateLast(model);
            await _myNoSqlServerDataWriter.InsertOrReplaceAsync(nosqlModel);
            _cachedLastOperation = model;
        }

        public async Task<HedgeOperation> GetLastAsync()
        {
            if (_cachedLastOperation != null)
            {
                return _cachedLastOperation;
            }

            var model = await _myNoSqlServerDataWriter.GetAsync(
                HedgeOperationNoSql.GeneratePartitionKey(),
                HedgeOperationNoSql.LastRowKey);
            _cachedLastOperation = model?.Value;

            return _cachedLastOperation;
        }
    }
}