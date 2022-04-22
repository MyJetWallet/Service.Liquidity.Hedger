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

    public ExchangesAnalyzer(
        ILogger<ExchangesAnalyzer> logger,
        IExternalMarket externalMarket
    )
    {
        _logger = logger;
        _externalMarket = externalMarket;
    }

    public async Task<ICollection<IndirectHedgeExchangeMarket>> FindIndirectMarketsAsync(string exchangeName,
        string transitAssetSymbol, string targetAssetSymbol, IEnumerable<HedgePairAsset> pairAssets)
    {
        _logger.LogInformation("FindIndirectMarkets markets started");

        var balancesResp = await _externalMarket.GetBalancesAsync(new GetBalancesRequest
        {
            ExchangeName = exchangeName
        });
        var marketInfosResp = await _externalMarket.GetMarketInfoListAsync(new GetMarketInfoListRequest
        {
            ExchangeName = exchangeName
        });
        var markets = new List<IndirectHedgeExchangeMarket>();

        _logger.LogInformation("GetExchangeMarkets {@ExchangeName}: {@Markets}", exchangeName,
            marketInfosResp?.Infos.Select(i => i.Market));
        _logger.LogInformation("GetExchangeBalances {@ExchangeName}: {@Markets}", exchangeName,
            balancesResp?.Balances);

        foreach (var pairAsset in pairAssets.OrderByDescending(a => a.Weight))
        {
            var transitMarketInfo = marketInfosResp?.Infos.FirstOrDefault(m =>
                m.BaseAsset == transitAssetSymbol && m.QuoteAsset == pairAsset.Symbol ||
                m.QuoteAsset == transitAssetSymbol && m.BaseAsset == pairAsset.Symbol);
            var transitPairAssetBalance = balancesResp?.Balances.FirstOrDefault(b => b.Symbol == pairAsset.Symbol);

            if (transitMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@PairAsset} is skipped.  Market with {@TransitAssetSymbol} not found",
                    pairAsset.Symbol, transitAssetSymbol);
                continue;
            }

            if (transitPairAssetBalance == null)
            {
                _logger.LogWarning("Market {@Market} with PairAsset {@PairAsset} is skipped. Balance not found",
                    transitMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            if (transitPairAssetBalance.Free <= 0)
            {
                _logger.LogWarning(
                    "Market {@Market} with PairAsset {@PairAsset} is skipped. FreeBalance on exchange is 0",
                    transitMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            var targetMarketInfo = marketInfosResp.Infos.FirstOrDefault(m =>
                m.BaseAsset == transitAssetSymbol && m.QuoteAsset == targetAssetSymbol ||
                m.QuoteAsset == transitAssetSymbol && m.BaseAsset == targetAssetSymbol);
            var targetPairAssetBalance = balancesResp.Balances.FirstOrDefault(b => b.Symbol == transitAssetSymbol);

            if (targetMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@PairAsset} is skipped.  Market with {@TargetAsset} not found",
                    pairAsset.Symbol, targetAssetSymbol);
                continue;
            }

            if (targetPairAssetBalance == null)
            {
                _logger.LogWarning("Market {@Market} with PairAsset {@PairAsset} is skipped. Balance not found",
                    targetMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            markets.Add(new IndirectHedgeExchangeMarket
            {
                ExchangeName = exchangeName,
                Weight = pairAsset.Weight,
                TransitMarketInfo = transitMarketInfo,
                TargetMarketInfo = targetMarketInfo,
                TransitAssetSymbol = transitAssetSymbol,
                TransitPairAssetSymbol = pairAsset.Symbol,
                TransitPairAssetAvailableVolume = Math.Min(pairAsset.AvailableVolume, transitPairAssetBalance.Free)
            });
        }

        var marketNames = string.Join(", ", markets.Select(m => m.GetMarketsDesc()));

        _logger.LogInformation(
            "FindPossible IndirectMarkets ended. Markets: {@Markets}, TransitAsset: {@TransitAsset}, TargetAsset: {@TargetAsset}",
            marketNames, transitAssetSymbol, targetAssetSymbol);

        return markets;
    }

    public async Task<ICollection<DirectHedgeExchangeMarket>> FindDirectMarketsAsync(string exchangeName,
        HedgeInstruction hedgeInstruction)
    {
        _logger.LogInformation("FindDirect markets started");

        var balancesResp = await _externalMarket.GetBalancesAsync(new GetBalancesRequest
        {
            ExchangeName = exchangeName
        });
        var marketInfosResp = await _externalMarket.GetMarketInfoListAsync(new GetMarketInfoListRequest
        {
            ExchangeName = exchangeName
        });
        var markets = new List<DirectHedgeExchangeMarket>();

        _logger.LogInformation("GetExchangeMarkets {@ExchangeName}: {@Markets}", exchangeName,
            marketInfosResp?.Infos.Select(i => i.Market));
        _logger.LogInformation("GetExchangeBalances {@ExchangeName}: {@Markets}", exchangeName,
            balancesResp?.Balances);

        foreach (var pairAsset in hedgeInstruction.PairAssets)
        {
            var exchangeMarketInfo = marketInfosResp?.Infos.FirstOrDefault(m =>
                m.BaseAsset == hedgeInstruction.TargetAssetSymbol && m.QuoteAsset == pairAsset.Symbol ||
                m.QuoteAsset == hedgeInstruction.TargetAssetSymbol && m.BaseAsset == pairAsset.Symbol);
            var exchangeBalance = balancesResp?.Balances.FirstOrDefault(b => b.Symbol == pairAsset.Symbol);

            if (exchangeMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@PairAsset} is skipped.  Market with {@TargetAsset} not found",
                    pairAsset.Symbol, hedgeInstruction.TargetAssetSymbol);
                continue;
            }

            if (exchangeBalance == null)
            {
                _logger.LogWarning("Market {@Market} with PairAsset {@PairAsset} is skipped. Balance not found",
                    exchangeMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            if (exchangeBalance.Free <= 0)
            {
                _logger.LogWarning("Market {@Market} with PairAsset {@PairAsset} is skipped. FreeBalance is 0",
                    exchangeMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            markets.Add(new DirectHedgeExchangeMarket
            {
                ExchangeName = exchangeName,
                Weight = pairAsset.Weight,
                Info = exchangeMarketInfo,
                AvailableVolume = Math.Min(pairAsset.AvailableVolume, exchangeBalance.Free)
            });
        }

        _logger.LogInformation("FindPossible DirectMarkets ended. Found markets: {@Markets}",
            string.Join(", ", markets.Select(m => m.Info.Market)));

        return markets;
    }
}