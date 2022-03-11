using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;

namespace Service.Liquidity.Hedger
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly ServiceBusLifeTime _myServiceBusTcpClient;
        private readonly MyNoSqlClientLifeTime _myNoSqlClientLifeTime;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime,
            ILogger<ApplicationLifetimeManager> logger,
            ServiceBusLifeTime myServiceBusTcpClient,
            MyNoSqlClientLifeTime myNoSqlClientLifeTime
        )
            : base(appLifetime)
        {
            _logger = logger;
            _myServiceBusTcpClient = myServiceBusTcpClient;
            _myNoSqlClientLifeTime = myNoSqlClientLifeTime;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _myServiceBusTcpClient.Start();
            _myNoSqlClientLifeTime.Start();
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _myServiceBusTcpClient.Stop();
            _myNoSqlClientLifeTime.Stop();
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}