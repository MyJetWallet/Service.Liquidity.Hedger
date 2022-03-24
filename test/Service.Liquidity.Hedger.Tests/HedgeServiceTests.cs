using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using NSubstitute;
using NUnit.Framework;
using Service.IndexPrices.Domain.Models;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Hedger.Domain.Services;

namespace Service.Liquidity.Hedger.Tests;

public class HedgeServiceTests
{
    private ILogger<HedgeService> _logger;
    private IExternalMarket _externalMarket;
    private IHedgeOperationsStorage _hedgeOperationsStorage;
    private ICurrentPricesCache _currentPricesCache;
    private IExchangesAnalyzer _exchangesAnalyzer;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<HedgeService>>();
        _externalMarket = Substitute.For<IExternalMarket>();
        _hedgeOperationsStorage = Substitute.For<IHedgeOperationsStorage>();
        _currentPricesCache = Substitute.For<ICurrentPricesCache>();
        _exchangesAnalyzer = Substitute.For<IExchangesAnalyzer>();
    }

    [Test]
    public async Task Hedge_NoMarkets_DoNotMakesTrades()
    {
        // arrange
        _exchangesAnalyzer.FindPossibleMarketsAsync(default).ReturnsForAnyArgs(new List<HedgeExchangeMarket>());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage, _currentPricesCache,
            _exchangesAnalyzer);
        var hedgeInstruction = new HedgeInstruction();

        // act
        var operation = await service.HedgeAsync(hedgeInstruction);

        // assert
        operation.TradedVolume.Should().Be(0);
    }
    
    [Test]
    public async Task Hedge_MakesBuyTrade_CalculatesTradedVolume()
    {
        // arrange
        var sellAsset = new HedgePairAsset
        {
            Symbol = "BTC"
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 1,
            TargetAssetSymbol = "XRP",
            PairAssets = new List<HedgePairAsset> { sellAsset }
        };

        var market = new HedgeExchangeMarket
        {
            AssetExchangeBalance = new ExchangeBalance
            {
                Free = 2
            },
            ExchangeMarketInfo = new ExchangeMarketInfo
            {
                BaseAsset = hedgeInstruction.TargetAssetSymbol,
                QuoteAsset = sellAsset.Symbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindPossibleMarketsAsync(default)
            .ReturnsForAnyArgs(new List<HedgeExchangeMarket> { market });
        _currentPricesCache.Get(default, default).ReturnsForAnyArgs(new CurrentPrice
        {
            Price = 1,
        });
        var exchangeTrade = new ExchangeTrade
        {
            Side = OrderSide.Buy,
            Volume = 1
        };
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(exchangeTrade);
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage, _currentPricesCache,
            _exchangesAnalyzer);

        // act
        var operation = await service.HedgeAsync(hedgeInstruction);

        // assert
        operation.TradedVolume.Should().Be(Convert.ToDecimal(exchangeTrade.Volume));
    }
    
    [Test]
    public async Task Hedge_MakesSellTrade_CalculatesTradedVolume()
    {
        // arrange
        var sellAsset = new HedgePairAsset
        {
            Symbol = "BTC"
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 1,
            TargetAssetSymbol = "XRP",
            PairAssets = new List<HedgePairAsset> { sellAsset }
        };

        var market = new HedgeExchangeMarket
        {
            AssetExchangeBalance = new ExchangeBalance
            {
                Free = 2
            },
            ExchangeMarketInfo = new ExchangeMarketInfo
            {
                BaseAsset = hedgeInstruction.TargetAssetSymbol,
                QuoteAsset = sellAsset.Symbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindPossibleMarketsAsync(default)
            .ReturnsForAnyArgs(new List<HedgeExchangeMarket> { market });
        _currentPricesCache.Get(default, default).ReturnsForAnyArgs(new CurrentPrice
        {
            Price = 1,
        });

        var exchangeTrade = new ExchangeTrade
        {
            Side = OrderSide.Sell,
            Volume = 1,
            Price = 1
        };
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(exchangeTrade);
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage, _currentPricesCache,
            _exchangesAnalyzer);

        // act
        var operation = await service.HedgeAsync(hedgeInstruction);

        // assert
        operation.TradedVolume.Should().Be(Convert.ToDecimal(exchangeTrade.Price * exchangeTrade.Volume));
    }

    [Test]
    public async Task Hedge_DirectMarketInBaseAssetWihEnoughBalance_MakesBuyTradeOnTargetVolume()
    {
        // arrange
        var sellAsset = new HedgePairAsset
        {
            Symbol = "BTC"
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 1,
            TargetAssetSymbol = "XRP",
            PairAssets = new List<HedgePairAsset> { sellAsset }
        };

        var market = new HedgeExchangeMarket
        {
            AssetExchangeBalance = new ExchangeBalance
            {
                Free = 2
            },
            ExchangeMarketInfo = new ExchangeMarketInfo
            {
                BaseAsset = hedgeInstruction.TargetAssetSymbol,
                QuoteAsset = sellAsset.Symbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindPossibleMarketsAsync(default)
            .ReturnsForAnyArgs(new List<HedgeExchangeMarket> { market });
        _currentPricesCache.Get(default, default).ReturnsForAnyArgs(new CurrentPrice
        {
            Price = 1,
        });
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage, _currentPricesCache,
            _exchangesAnalyzer);

        // act
        await service.HedgeAsync(hedgeInstruction);

        // assert
        await _externalMarket.Received().MarketTrade(
            Arg.Is<MarketTradeRequest>(request =>
                request.Side == OrderSide.Buy &&
                Convert.ToDecimal(request.Volume) == hedgeInstruction.TargetVolume));
    }
    
    [Test]
    public async Task Hedge_DirectMarketInQuoteAssetWihEnoughBalance_MakesSellTradeOnTargetVolume()
    {
        // arrange
        var sellAsset = new HedgePairAsset
        {
            Symbol = "BTC"
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 1,
            TargetAssetSymbol = "XRP",
            PairAssets = new List<HedgePairAsset> { sellAsset }
        };

        var market = new HedgeExchangeMarket
        {
            AssetExchangeBalance = new ExchangeBalance
            {
                Free = 2
            },
            ExchangeMarketInfo = new ExchangeMarketInfo
            {
                BaseAsset = sellAsset.Symbol,
                QuoteAsset = hedgeInstruction.TargetAssetSymbol ,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindPossibleMarketsAsync(default)
            .ReturnsForAnyArgs(new List<HedgeExchangeMarket> { market });
        _currentPricesCache.Get(default, default).ReturnsForAnyArgs(new CurrentPrice
        {
            Price = 1,
        });
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage, _currentPricesCache,
            _exchangesAnalyzer);

        // act
        await service.HedgeAsync(hedgeInstruction);

        // assert
        await _externalMarket.Received().MarketTrade(
            Arg.Is<MarketTradeRequest>(request =>
                request.Side == OrderSide.Sell &&
                Convert.ToDecimal(request.Volume) == hedgeInstruction.TargetVolume));
    }
    
    [Test]
    public async Task Hedge_DirectMarketInQuoteAssetWithoutEnoughBalance_MakesSellTradeOnPossibleVolume()
    {
        // arrange
        var sellAsset = new HedgePairAsset
        {
            Symbol = "BTC"
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 110,
            TargetAssetSymbol = "XRP",
            PairAssets = new List<HedgePairAsset> { sellAsset }
        };

        var market = new HedgeExchangeMarket
        {
            AssetExchangeBalance = new ExchangeBalance
            {
                Free = 100
            },
            ExchangeMarketInfo = new ExchangeMarketInfo
            {
                BaseAsset = sellAsset.Symbol,
                QuoteAsset = hedgeInstruction.TargetAssetSymbol ,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindPossibleMarketsAsync(default)
            .ReturnsForAnyArgs(new List<HedgeExchangeMarket> { market });
        _currentPricesCache.Get(default, default).ReturnsForAnyArgs(new CurrentPrice
        {
            Price = 1,
        });
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage, _currentPricesCache,
            _exchangesAnalyzer);

        // act
        await service.HedgeAsync(hedgeInstruction);

        // assert
        await _externalMarket.Received().MarketTrade(
            Arg.Is<MarketTradeRequest>(request =>
                request.Side == OrderSide.Sell &&
                request.Volume == 90));
    }
    
    [Test]
    public async Task Hedge_DirectMarketInBaseAssetWithoutEnoughBalance_MakesBuyTradeOnPossibleVolume()
    {
        // arrange
        var sellAsset = new HedgePairAsset
        {
            Symbol = "BTC"
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 110,
            TargetAssetSymbol = "XRP",
            PairAssets = new List<HedgePairAsset> { sellAsset }
        };

        var market = new HedgeExchangeMarket
        {
            AssetExchangeBalance = new ExchangeBalance
            {
                Free = 100
            },
            ExchangeMarketInfo = new ExchangeMarketInfo
            {
                BaseAsset = hedgeInstruction.TargetAssetSymbol,
                QuoteAsset = sellAsset.Symbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindPossibleMarketsAsync(default)
            .ReturnsForAnyArgs(new List<HedgeExchangeMarket> { market });
        _currentPricesCache.Get(default, default).ReturnsForAnyArgs(new CurrentPrice
        {
            Price = 1,
        });
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage, _currentPricesCache,
            _exchangesAnalyzer);

        // act
        await service.HedgeAsync(hedgeInstruction);

        // assert
        await _externalMarket.Received().MarketTrade(
            Arg.Is<MarketTradeRequest>(request =>
                request.Side == OrderSide.Buy &&
                request.Volume == 90));
    }
}