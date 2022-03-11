using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.Liquidity.Hedger.Settings
{
    public class SettingsModel
    {
        [YamlProperty("LiquidityHedger.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("LiquidityHedger.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("LiquidityHedger.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("LiquidityHedger.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("LiquidityHedger.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }

        [YamlProperty("LiquidityHedger.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("LiquidityHedger.ExternalApiGrpcUrl")]
        public string ExternalApiGrpcUrl { get; set; }
    }
}