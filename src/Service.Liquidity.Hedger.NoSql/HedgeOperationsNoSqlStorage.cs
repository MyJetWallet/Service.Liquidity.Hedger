using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql
{
    public class HedgeOperationsNoSqlStorage : IHedgeOperationsStorage
    {
        private readonly IMyNoSqlServerDataWriter<HedgeOperationNoSql> _myNoSqlServerDataWriter;

        public HedgeOperationsNoSqlStorage(
            IMyNoSqlServerDataWriter<HedgeOperationNoSql> myNoSqlServerDataWriter
        )
        {
            _myNoSqlServerDataWriter = myNoSqlServerDataWriter;
        }

        public async Task AddOrUpdateLastAsync(HedgeOperation model)
        {
            var nosqlModel = HedgeOperationNoSql.CreateLast(model);
            await _myNoSqlServerDataWriter.InsertOrReplaceAsync(nosqlModel);
        }

        public async Task<HedgeOperation> GetLastAsync()
        {
            var model = await _myNoSqlServerDataWriter.GetAsync(HedgeOperationNoSql.GeneratePartitionKey(),
                HedgeOperationNoSql.LastRowKey);

            return model?.Value;
        }
    }
}