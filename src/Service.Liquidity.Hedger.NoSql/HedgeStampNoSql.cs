using MyNoSqlServer.Abstractions;
using Service.Liquidity.Monitoring.Domain.Models.Hedging;

namespace Service.Liquidity.Hedger.NoSql
{
    public class HedgeStampNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-liquidity-hedgestamp";
        public static string GeneratePartitionKey() => "*";
        public static string GenerateRowKey() => "*";

        public HedgeStamp Value { get; set; }

        public static HedgeStampNoSql Create(HedgeStamp src)
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