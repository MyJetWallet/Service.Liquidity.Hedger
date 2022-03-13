﻿using System.Collections.Generic;
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

    public MarketsAnalyzer(
        ILogger<MarketsAnalyzer> logger,
        IExternalMarket externalMarket
    )
    {
        _logger = logger;
        _externalMarket = externalMarket;
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

        foreach (var sellAsset in hedgeInstruction.SellAssets)
        {
            var marketInfo = marketInfosResp.Infos
                .FirstOrDefault(m =>
                    m.BaseAsset == hedgeInstruction.BuyAssetSymbol && m.QuoteAsset == sellAsset.Symbol);
            var exchangeBalance = balancesResp.Balances.FirstOrDefault(b => b.Symbol == sellAsset.Symbol);

            if (marketInfo == null || exchangeBalance == null)
            {
                _logger.LogWarning("SellAsset {@sellAsset} is skipped. {@marketInfo} {@exchangeBalance}",
                    sellAsset, marketInfo, exchangeBalance);
                continue;
            }

            markets.Add(new HedgeExchangeMarket
            {
                ExchangeName = ExchangeName,
                Weight = sellAsset.Weight,
                ExchangeBalance = exchangeBalance,
                ExchangeMarketInfo = marketInfo
            });
        }

        return markets;
    }
}