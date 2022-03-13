using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql
{
    public class HedgeOperationNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-liquidity-hedge-operations";
        public static string GeneratePartitionKey() => "*";
        public static string GenerateRowKey(string id) => id;
        public static string LastRowKey { get; } = "last";

        public HedgeOperation Value { get; set; }

        public static HedgeOperationNoSql CreateLast(HedgeOperation src)
        {
            return new()
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = LastRowKey,
                Value = src
            };
        }
    }
}