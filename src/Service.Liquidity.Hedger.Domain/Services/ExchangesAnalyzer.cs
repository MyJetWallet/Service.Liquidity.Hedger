using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;

namespace Service.Liquidity.Hedger.Domain.Services;

public class ExchangesAnalyzer : IExchangesAnalyzer
{
    private readonly ILogger<ExchangesAnalyzer> _logger;
    private readonly IExternalMarket _externalMarket;
    private const string ExchangeName = "FTX";

    public ExchangesAnalyzer(
        ILogger<ExchangesAnalyzer> logger,
        IExternalMarket externalMarket
    )
    {
        _logger = logger;
        _externalMarket = externalMarket;
    }

    public async Task<ICollection<HedgeExchangeMarket>> FindPossibleMarketsAsync(HedgeInstruction hedgeInstruction)
    {
        _logger.LogInformation("FindPossible markets started");
        
        var balancesResp = await _externalMarket.GetBalancesAsync(new GetBalancesRequest
        {
            ExchangeName = ExchangeName
        });
        var marketInfosResp = await _externalMarket.GetMarketInfoListAsync(new GetMarketInfoListRequest
        {
            ExchangeName = ExchangeName
        });
        var markets = new List<HedgeExchangeMarket>();

        _logger.LogInformation("GetExchangeMarkets {@exchangeName}: {@markets}", ExchangeName,
            marketInfosResp?.Infos.Select(i => i.Market));
        _logger.LogInformation("GetExchangeBalances {@exchangeName}: {@markets}", ExchangeName,
            balancesResp?.Balances);

        foreach (var sellAsset in hedgeInstruction.PairAssets)
        {
            var exchangeMarketInfo = marketInfosResp?.Infos.FirstOrDefault(m =>
                m.BaseAsset == hedgeInstruction.TargetAssetSymbol && m.QuoteAsset == sellAsset.Symbol ||
                m.QuoteAsset == hedgeInstruction.TargetAssetSymbol && m.BaseAsset == sellAsset.Symbol);
            var exchangeBalance = balancesResp?.Balances.FirstOrDefault(b => b.Symbol == sellAsset.Symbol);

            if (exchangeMarketInfo == null || exchangeBalance == null)
            {
                _logger.LogWarning(
                    "QuoteAsset {@quoteAsset} is skipped. Market {@market}; ExchangeBalance={@exchangeBalance}",
                    sellAsset.Symbol, exchangeMarketInfo?.Market, exchangeBalance);
                continue;
            }

            if (exchangeBalance.Free <= 0)
            {
                _logger.LogWarning("QuoteAsset {@quoteAsset} is skipped. FreeBalance on exchange is 0", sellAsset.Symbol);
                continue;
            }

            markets.Add(new HedgeExchangeMarket
            {
                ExchangeName = ExchangeName,
                Weight = sellAsset.Weight,
                AssetExchangeBalance = exchangeBalance,
                ExchangeMarketInfo = exchangeMarketInfo
            });
        }

        _logger.LogInformation("FindPossible markets ended. Found markets: {@markets} for HedgeInstruction {@hedgeInstruction}",
            markets, hedgeInstruction);

        return markets;
    }
}