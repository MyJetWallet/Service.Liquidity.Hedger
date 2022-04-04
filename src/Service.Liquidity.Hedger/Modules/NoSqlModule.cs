using Autofac;
using MyJetWallet.Sdk.NoSql;
using Service.IndexPrices.Domain.Models;
using Service.Liquidity.Hedger.NoSql;
using Service.Liquidity.Hedger.NoSql.HedgeInstructions;
using Service.Liquidity.Hedger.NoSql.Settings;

namespace Service.Liquidity.Hedger.Modules
{
    public class NoSqlModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var noSqlClient = builder.CreateNoSqlClient(Program.ReloadedSettings(e => e.MyNoSqlReaderHostPort));

            builder.RegisterMyNoSqlWriter<HedgeOperationNoSql>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl),
                HedgeOperationNoSql.TableName);
            builder.RegisterMyNoSqlReader<CurrentPricesNoSql>(noSqlClient, CurrentPricesNoSql.TableName);
            builder.RegisterMyNoSqlWriter<HedgeSettingsNoSql>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl),
                HedgeSettingsNoSql.TableName);
            builder.RegisterMyNoSqlWriter<HedgeInstructionNoSql>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl),
                HedgeInstructionNoSql.TableName);
        }
    }
}