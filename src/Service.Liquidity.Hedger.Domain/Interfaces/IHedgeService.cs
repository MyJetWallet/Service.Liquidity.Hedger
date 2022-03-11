using System.Threading.Tasks;
using Service.Liquidity.Monitoring.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface IHedgeService
    {
        public Task HedgeAsync(PortfolioMonitoringMessage message);
    }
}