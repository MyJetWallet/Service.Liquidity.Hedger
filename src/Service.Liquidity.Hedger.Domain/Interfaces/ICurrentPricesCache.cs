using Service.IndexPrices.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Interfaces
{
    public interface ICurrentPricesCache
    {
        CurrentPrice Get(string source, string market);
    }
}