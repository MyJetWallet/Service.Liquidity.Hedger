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
            var noSqlClient = builder.CreateNoSqlClient(() => Program.Settings.MyNoSqlReaderHostPort);

            builder.RegisterMyNoSqlWriter<HedgeOperationNoSql>(() => Program.Settings.MyNoSqlReaderHostPort,
                HedgeOperationNoSql.TableName);
            builder.RegisterMyNoSqlReader<CurrentPricesNoSql>(noSqlClient, CurrentPricesNoSql.TableName);
            builder.RegisterMyNoSqlWriter<HedgeSettingsNoSql>(() => Program.Settings.MyNoSqlReaderHostPort,
                HedgeSettingsNoSql.TableName);
            builder.RegisterMyNoSqlWriter<HedgeInstructionNoSql>(() => Program.Settings.MyNoSqlReaderHostPort,
                HedgeInstructionNoSql.TableName);
        }
    }
}