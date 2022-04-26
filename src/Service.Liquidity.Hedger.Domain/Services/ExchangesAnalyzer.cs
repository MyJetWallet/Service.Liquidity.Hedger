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

    public async Task<ICollection<IndirectHedgeExchangeMarket>> FindIndirectMarketsToBuyAssetAsync(string exchangeName,
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
            var firstTradeMarketInfo = marketInfosResp?.Infos.FirstOrDefault(m =>
                m.BaseAsset == transitAssetSymbol && m.QuoteAsset == pairAsset.Symbol ||
                m.QuoteAsset == transitAssetSymbol && m.BaseAsset == pairAsset.Symbol);

            if (firstTradeMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@PairAsset} is skipped.  Market with {@TransitAssetSymbol} not found",
                    pairAsset.Symbol, transitAssetSymbol);
                continue;
            }

            var pairAssetBalance = balancesResp?.Balances.FirstOrDefault(b => b.Symbol == pairAsset.Symbol);

            if (pairAssetBalance == null)
            {
                _logger.LogWarning("Market {@Market} with PairAsset {@PairAsset} is skipped. Balance not found",
                    firstTradeMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            if (pairAssetBalance.Free <= 0)
            {
                _logger.LogWarning(
                    "Market {@Market} with PairAsset {@PairAsset} is skipped. FreeBalance on exchange is 0",
                    firstTradeMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            var secondTradeMarketInfo = marketInfosResp.Infos.FirstOrDefault(m =>
                m.BaseAsset == transitAssetSymbol && m.QuoteAsset == targetAssetSymbol ||
                m.QuoteAsset == transitAssetSymbol && m.BaseAsset == targetAssetSymbol);

            if (secondTradeMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@PairAsset} is skipped.  Market with {@TargetAsset} not found",
                    pairAsset.Symbol, targetAssetSymbol);
                continue;
            }

            markets.Add(new IndirectHedgeExchangeMarket
            {
                ExchangeName = exchangeName,
                Weight = pairAsset.Weight,
                FirstTradeMarketInfo = firstTradeMarketInfo,
                SecondTradeMarketInfo = secondTradeMarketInfo,
                TransitAssetSymbol = transitAssetSymbol,
                PairAssetSymbol = pairAsset.Symbol,
                PairAssetAvailableVolume = Math.Min(pairAsset.AvailableVolume, pairAssetBalance.Free)
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

            if (exchangeMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@PairAsset} is skipped.  Market with {@TargetAsset} not found",
                    pairAsset.Symbol, hedgeInstruction.TargetAssetSymbol);
                continue;
            }

            var pairAssetBalance = balancesResp?.Balances.FirstOrDefault(b => b.Symbol == pairAsset.Symbol);

            if (pairAssetBalance == null)
            {
                _logger.LogWarning(
                    "Market {@Market} with PairAsset {@PairAsset} is skipped. Pair asset balance not found",
                    exchangeMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            var targetAssetBalance =
                balancesResp.Balances.FirstOrDefault(b => b.Symbol == hedgeInstruction.TargetAssetSymbol);

            if (targetAssetBalance == null)
            {
                _logger.LogWarning(
                    "Market {@Market} with PairAsset {@PairAsset} is skipped. Target asset balance not found",
                    exchangeMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            markets.Add(new DirectHedgeExchangeMarket
            {
                ExchangeName = exchangeName,
                Weight = pairAsset.Weight,
                Info = exchangeMarketInfo,
                AvailablePairAssetVolume = Math.Min(pairAsset.AvailableVolume, pairAssetBalance.Free),
                AvailableTargetAssetVolume = Math.Min(hedgeInstruction.TargetVolume, targetAssetBalance.Free)
            });
        }

        _logger.LogInformation("FindPossible DirectMarkets ended. Found markets: {@Markets}",
            string.Join(", ", markets.Select(m => m.Info.Market)));

        return markets;
    }
}