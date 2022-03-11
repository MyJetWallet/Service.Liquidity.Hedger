using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql
{
    public class HedgeStampNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-liquidity-hedgestamp";
        public static string GeneratePartitionKey() => "*";
        public static string GenerateRowKey() => "*";

        public HedgeOperationId Value { get; set; }

        public static HedgeStampNoSql Create(HedgeOperationId src)
        {
            return new()
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(),
                Value = src
            };
        }
    }
}