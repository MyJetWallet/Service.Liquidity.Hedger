using Autofac;
using MyJetWallet.Sdk.NoSql;
using Service.IndexPrices.Domain.Models;
using Service.Liquidity.Hedger.NoSql;

namespace Service.Liquidity.Hedger.Modules
{
    public class NoSqlModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var noSqlClient = builder.CreateNoSqlClient(Program.ReloadedSettings(e => e.MyNoSqlReaderHostPort));

            builder.RegisterMyNoSqlWriter<HedgeStampNoSql>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl),
                HedgeStampNoSql.TableName);
            builder.RegisterMyNoSqlReader<CurrentPricesNoSql>(noSqlClient, CurrentPricesNoSql.TableName);
        }
    }
}