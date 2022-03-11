using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.Liquidity.Hedger.Settings
{
    public class SettingsModel
    {
        [YamlProperty("Liquidity.Hedger.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("Liquidity.Hedger.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("Liquidity.Hedger.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("LiquidityMonitoring.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("LiquidityMonitoring.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }

        [YamlProperty("LiquidityMonitoring.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("LiquidityMonitoring.ExternalApiGrpcUrl")]
        public string ExternalApiGrpcUrl { get; set; }
    }
}