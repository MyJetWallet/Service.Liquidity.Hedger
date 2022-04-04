using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql.HedgeInstructions
{
    public class HedgeInstructionNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-liquidity-hedge-instructions";
        public static string GeneratePartitionKey() => "*";
        public static string GenerateRowKey(string id) => id;
        public HedgeInstruction Value { get; set; }

        public static HedgeInstructionNoSql Create(HedgeInstruction src)
        {
            return new()
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(src.MonitoringRuleId),
                Value = src
            };
        }
    }
}