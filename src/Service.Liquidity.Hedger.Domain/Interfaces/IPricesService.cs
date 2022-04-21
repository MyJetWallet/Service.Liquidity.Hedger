using System.Threading.Tasks;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Hedger.Domain.Interfaces;

public interface IPricesService
{
    public Task<decimal> GetConvertPriceAsync(string exchangeName, string market);
    public Task<decimal> GetLimitTradePriceAsync(string exchangeName, string market, OrderSide orderSide);
}