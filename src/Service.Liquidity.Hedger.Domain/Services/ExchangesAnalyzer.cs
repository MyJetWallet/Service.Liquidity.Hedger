﻿using System;
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

    public async Task<ICollection<IndirectHedgeExchangeMarket>> FindIndirectMarketsAsync(
        string transitAssetSymbol, string targetAssetSymbol, IEnumerable<HedgePairAsset> pairAssets)
    {
        _logger.LogInformation("FindIndirectMarkets markets started");

        var balancesResp = await _externalMarket.GetBalancesAsync(new GetBalancesRequest
        {
            ExchangeName = ExchangeName
        });
        var marketInfosResp = await _externalMarket.GetMarketInfoListAsync(new GetMarketInfoListRequest
        {
            ExchangeName = ExchangeName
        });
        var markets = new List<IndirectHedgeExchangeMarket>();

        _logger.LogInformation("GetExchangeMarkets {@exchangeName}: {@markets}", ExchangeName,
            marketInfosResp?.Infos.Select(i => i.Market));
        _logger.LogInformation("GetExchangeBalances {@exchangeName}: {@markets}", ExchangeName,
            balancesResp?.Balances);

        foreach (var pairAsset in pairAssets)
        {
            var transitMarketInfo = marketInfosResp?.Infos.FirstOrDefault(m =>
                m.BaseAsset == transitAssetSymbol && m.QuoteAsset == pairAsset.Symbol ||
                m.QuoteAsset == transitAssetSymbol && m.BaseAsset == pairAsset.Symbol);
            var transitPairAssetBalance = balancesResp?.Balances.FirstOrDefault(b => b.Symbol == pairAsset.Symbol);
            
            if (transitMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@pairAsset} is skipped.  Market with {@targetAsset} not found", 
                    pairAsset.Symbol, targetAssetSymbol);
                continue;
            }
            
            if (transitPairAssetBalance == null)
            {
                _logger.LogWarning("Market {@market} with PairAsset {@pairAsset} is skipped. Balance not found",
                    transitMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            if (transitPairAssetBalance.Free <= 0)
            {
                _logger.LogWarning("Market {@market} with PairAsset {@quoteAsset} is skipped. FreeBalance on exchange is 0",
                    transitMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            var targetMarketInfo = marketInfosResp.Infos.FirstOrDefault(m =>
                m.BaseAsset == transitAssetSymbol && m.QuoteAsset == targetAssetSymbol ||
                m.QuoteAsset == transitAssetSymbol && m.BaseAsset == targetAssetSymbol);
            var targetPairAssetBalance = balancesResp.Balances.FirstOrDefault(b => b.Symbol == transitAssetSymbol);
            
            if (targetMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@pairAsset} is skipped.  Market with {@targetAsset} not found", 
                    pairAsset.Symbol, targetAssetSymbol);
                continue;
            }
            
            if (targetPairAssetBalance == null)
            {
                _logger.LogWarning("Market {@market} with PairAsset {@pairAsset} is skipped. Balance not found",
                    targetMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            markets.Add(new IndirectHedgeExchangeMarket
            {
                ExchangeName = ExchangeName,
                Weight = pairAsset.Weight,
                TransitPairAssetBalance = transitPairAssetBalance,
                TransitMarketInfo = transitMarketInfo,
                TargetPairAssetBalance = targetPairAssetBalance,
                TargetMarketInfo = targetMarketInfo,
                TransitAssetSymbol = transitAssetSymbol
            });
        }
        
        _logger.LogInformation(
            "FindPossible IndirectMarkets ended. Found markets: {@markets}. TransitAsset: {@transitAsset}, TargetAsset: {@targetAsset}",
            string.Join(", ", markets.Select(m => m.TargetMarketInfo.Market)), transitAssetSymbol, targetAssetSymbol);

        return markets;
    }

    public async Task<ICollection<DirectHedgeExchangeMarket>> FindDirectMarketsAsync(HedgeInstruction hedgeInstruction)
    {
        _logger.LogInformation("FindDirect markets started");

        var balancesResp = await _externalMarket.GetBalancesAsync(new GetBalancesRequest
        {
            ExchangeName = ExchangeName
        });
        var marketInfosResp = await _externalMarket.GetMarketInfoListAsync(new GetMarketInfoListRequest
        {
            ExchangeName = ExchangeName
        });
        var markets = new List<DirectHedgeExchangeMarket>();

        _logger.LogInformation("GetExchangeMarkets {@exchangeName}: {@markets}", ExchangeName,
            marketInfosResp?.Infos.Select(i => i.Market));
        _logger.LogInformation("GetExchangeBalances {@exchangeName}: {@markets}", ExchangeName,
            balancesResp?.Balances);

        foreach (var pairAsset in hedgeInstruction.PairAssets)
        {
            var exchangeMarketInfo = marketInfosResp?.Infos.FirstOrDefault(m =>
                m.BaseAsset == hedgeInstruction.TargetAssetSymbol && m.QuoteAsset == pairAsset.Symbol ||
                m.QuoteAsset == hedgeInstruction.TargetAssetSymbol && m.BaseAsset == pairAsset.Symbol);
            var exchangeBalance = balancesResp?.Balances.FirstOrDefault(b => b.Symbol == pairAsset.Symbol);

            if (exchangeMarketInfo == null)
            {
                _logger.LogWarning("PairAsset {@pairAsset} is skipped.  Market with {@targetAsset} not found", 
                    pairAsset.Symbol, hedgeInstruction.TargetAssetSymbol);
                continue;
            }
            
            if (exchangeBalance == null)
            {
                _logger.LogWarning("Market {@market} with PairAsset {@pairAsset} is skipped. Balance not found",
                    exchangeMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            if (exchangeBalance.Free <= 0)
            {
                _logger.LogWarning("Market {@market} with PairAsset {@quoteAsset} is skipped. FreeBalance is 0",
                    exchangeMarketInfo.Market, pairAsset.Symbol);
                continue;
            }

            markets.Add(new DirectHedgeExchangeMarket
            {
                ExchangeName = ExchangeName,
                Weight = pairAsset.Weight,
                Balance = exchangeBalance,
                Info = exchangeMarketInfo
            });
        }

        _logger.LogInformation(
            "FindPossible DirectMarkets ended. Found markets: {@markets} for HedgeInstruction {@hedgeInstruction}",
            string.Join(", ", markets.Select(m => m.Info.Market)), hedgeInstruction);

        return markets;
    }
}