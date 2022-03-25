using MyNoSqlServer.Abstractions;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.NoSql.Settings;

public class HedgeSettingsNoSql : MyNoSqlDbEntity
{
    public const string TableName = "myjetwallet-liquidity-hedge-settings";

    public static string GeneratePartitionKey() => "*";
    public static string GenerateRowKey() => "*";
    public HedgeSettings Value { get; set; }

    public static HedgeSettingsNoSql Create(HedgeSettings src)
    {
        return new HedgeSettingsNoSql
        {
            PartitionKey = GeneratePartitionKey(),
            RowKey = GenerateRowKey(),
            Value = src
        };
    }
}