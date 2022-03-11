using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql
{
    public class HedgeStampNoSqlStorage : IHedgeStampStorage
    {
        private readonly IMyNoSqlServerDataWriter<HedgeStampNoSql> _myNoSqlServerDataWriter;

        public HedgeStampNoSqlStorage(
            IMyNoSqlServerDataWriter<HedgeStampNoSql> myNoSqlServerDataWriter
        )
        {
            _myNoSqlServerDataWriter = myNoSqlServerDataWriter;
        }

        public async Task AddOrUpdateAsync(HedgeOperationId model)
        {
            var nosqlModel = HedgeStampNoSql.Create(model);
            await _myNoSqlServerDataWriter.InsertOrReplaceAsync(nosqlModel);
        }

        public async Task<HedgeOperationId> GetAsync()
        {
            var model = await _myNoSqlServerDataWriter.GetAsync(HedgeStampNoSql.GeneratePartitionKey(),
                HedgeStampNoSql.GenerateRowKey());

            return model?.Value;
        }
    }
}