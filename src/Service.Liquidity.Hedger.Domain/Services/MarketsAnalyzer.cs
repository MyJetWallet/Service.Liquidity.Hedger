using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services;

public class MarketsAnalyzer : IMarketsAnalyzer
{
    private readonly ILogger<MarketsAnalyzer> _logger;
    private readonly IExternalMarket _externalMarket;
    private const string ExchangeName = "FTX";
    private readonly ICurrentPricesCache _currentPricesCache;

    public MarketsAnalyzer(
        ILogger<MarketsAnalyzer> logger,
        IExternalMarket externalMarket,
        ICurrentPricesCache currentPricesCache
    )
    {
        _logger = logger;
        _externalMarket = externalMarket;
        _currentPricesCache = currentPricesCache;
    }

    public async Task<ICollection<HedgeExchangeMarket>> FindPossibleAsync(HedgeInstruction hedgeInstruction)
    {
        var balancesResp = await _externalMarket.GetBalancesAsync(new GetBalancesRequest
        {
            ExchangeName = ExchangeName
        });
        var marketInfosResp = await _externalMarket.GetMarketInfoListAsync(new GetMarketInfoListRequest
        {
            ExchangeName = ExchangeName
        });
        var markets = new List<HedgeExchangeMarket>();

        foreach (var quoteAsset in hedgeInstruction.QuoteAssets)
        {
            var exchangeMarketInfo = marketInfosResp.Infos.FirstOrDefault(m =>
                m.BaseAsset == hedgeInstruction.BaseAssetSymbol &&
                m.QuoteAsset == quoteAsset.Symbol);
            var exchangeBalance = balancesResp.Balances.FirstOrDefault(b => b.Symbol == quoteAsset.Symbol);

            if (exchangeMarketInfo == null || exchangeBalance == null)
            {
                _logger.LogWarning(
                    "QuoteAsset {@quoteAsset} is skipped. Market {@marketInfo}; ExchangeBalance={@exchangeBalance}",
                    quoteAsset.Symbol, exchangeMarketInfo?.Market, exchangeBalance);
                continue;
            }

            if (exchangeBalance.Free <= 0)
            {
                _logger.LogWarning("QuoteAsset {@quoteAsset} is skipped. Free balance on exchange is 0", quoteAsset.Symbol);
                continue;
            }

            var marketPrice = _currentPricesCache.Get(ExchangeName, exchangeMarketInfo.Market);
            var possibleVolumeToSell = exchangeBalance.Free * marketPrice.Price;

            if (possibleVolumeToSell < hedgeInstruction.TargetVolume)
            {
                _logger.LogWarning(
                    "QuoteAsset {@quoteAsset} is skipped. Not enough free balance: {@price} {@possibleVolume} {@targetVolume}",
                    quoteAsset.Symbol, marketPrice, possibleVolumeToSell, hedgeInstruction.TargetVolume);
                continue;
            }

            markets.Add(new HedgeExchangeMarket
            {
                ExchangeName = ExchangeName,
                Weight = quoteAsset.Weight,
                QuoteAssetExchangeBalance = exchangeBalance,
                ExchangeMarketInfo = exchangeMarketInfo
            });
        }

        _logger.LogInformation("Found possible markets {@markets} for HedgeInstruction {@hedgeInstruction}",
            markets, hedgeInstruction);

        return markets;
    }
}