using Autofac;
using MyJetWallet.Sdk.NoSql;
using Service.IndexPrices.Client;
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
            var noSqlClient = builder.CreateNoSqlClient(() => Program.Settings.MyNoSqlReaderHostPort);

            builder.RegisterMyNoSqlWriter<HedgeOperationNoSql>(() => Program.Settings.MyNoSqlWriterUrl,
                HedgeOperationNoSql.TableName);
            builder.RegisterMyNoSqlReader<CurrentPricesNoSql>(noSqlClient, CurrentPricesNoSql.TableName);
            builder.RegisterMyNoSqlWriter<HedgeSettingsNoSql>(() => Program.Settings.MyNoSqlWriterUrl,
                HedgeSettingsNoSql.TableName);
            builder.RegisterMyNoSqlWriter<HedgeInstructionNoSql>(() => Program.Settings.MyNoSqlWriterUrl,
                HedgeInstructionNoSql.TableName);
            builder.RegisterMyNoSqlReader<HedgeInstructionNoSql>(noSqlClient, HedgeInstructionNoSql.TableName);
        }
    }
}