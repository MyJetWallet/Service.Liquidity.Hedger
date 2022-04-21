using System;
using System.Collections.Generic;
using System.Linq;
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
    private IPricesService _pricesService;
    private IExchangesAnalyzer _exchangesAnalyzer;
    private IHedgeSettingsStorage _hedgeSettingsStorage;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<HedgeService>>();
        _externalMarket = Substitute.For<IExternalMarket>();
        _hedgeOperationsStorage = Substitute.For<IHedgeOperationsStorage>();
        _pricesService = Substitute.For<IPricesService>();
        _exchangesAnalyzer = Substitute.For<IExchangesAnalyzer>();
        _hedgeSettingsStorage = Substitute.For<IHedgeSettingsStorage>();
        _hedgeSettingsStorage.GetAsync().ReturnsForAnyArgs(new HedgeSettings
            { EnabledExchanges = new List<string> { "exchange" } });
    }

    [Test]
    public async Task Hedge_NoMarkets_DoNotMakesTrades()
    {
        // arrange
        _exchangesAnalyzer.FindDirectMarketsAsync(default, default)
            .ReturnsForAnyArgs(new List<DirectHedgeExchangeMarket>());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage,
            _exchangesAnalyzer, _hedgeSettingsStorage, _pricesService);
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

        var market = new DirectHedgeExchangeMarket
        {
            Balance = new ExchangeBalance
            {
                Free = 2
            },
            Info = new ExchangeMarketInfo
            {
                BaseAsset = hedgeInstruction.TargetAssetSymbol,
                QuoteAsset = sellAsset.Symbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindDirectMarketsAsync(default, default)
            .ReturnsForAnyArgs(new List<DirectHedgeExchangeMarket> { market });
        _pricesService.GetConvertPriceAsync(default, default).ReturnsForAnyArgs(0.5m);
        var exchangeTrade = new ExchangeTrade
        {
            Side = OrderSide.Buy,
            Volume = Convert.ToDouble(hedgeInstruction.TargetVolume)
        };
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(exchangeTrade);
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage,
            _exchangesAnalyzer, _hedgeSettingsStorage, _pricesService);

        // act
        var operation = await service.HedgeAsync(hedgeInstruction);

        // assert
        operation.TradedVolume.Should().Be(Convert.ToDecimal(exchangeTrade.Volume));
        operation.HedgeTrades.First().Side.Should().Be(OrderSide.Buy);
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

        var market = new DirectHedgeExchangeMarket
        {
            Balance = new ExchangeBalance
            {
                Free = 2
            },
            Info = new ExchangeMarketInfo
            {
                BaseAsset = sellAsset.Symbol,
                QuoteAsset = hedgeInstruction.TargetAssetSymbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindDirectMarketsAsync(default, default)
            .ReturnsForAnyArgs(new List<DirectHedgeExchangeMarket> { market });
        var price = 0.5m;
        _pricesService.GetConvertPriceAsync(default, default).ReturnsForAnyArgs(price);

        var exchangeTrade = new ExchangeTrade
        {
            Side = OrderSide.Sell,
            Volume = 1,
            Price = Convert.ToDouble(price)
        };
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(exchangeTrade);
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage,
            _exchangesAnalyzer, _hedgeSettingsStorage, _pricesService);

        // act
        var operation = await service.HedgeAsync(hedgeInstruction);

        // assert
        operation.TradedVolume.Should().Be(Convert.ToDecimal(exchangeTrade.Price * exchangeTrade.Volume));
        operation.HedgeTrades.First().Side.Should().Be(OrderSide.Sell);
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

        var market = new DirectHedgeExchangeMarket
        {
            Balance = new ExchangeBalance
            {
                Free = 2
            },
            Info = new ExchangeMarketInfo
            {
                BaseAsset = hedgeInstruction.TargetAssetSymbol,
                QuoteAsset = sellAsset.Symbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindDirectMarketsAsync(default, default)
            .ReturnsForAnyArgs(new List<DirectHedgeExchangeMarket> { market });
        _pricesService.GetConvertPriceAsync(default, default).ReturnsForAnyArgs(1);
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage,
            _exchangesAnalyzer, _hedgeSettingsStorage, _pricesService);

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

        var market = new DirectHedgeExchangeMarket
        {
            Balance = new ExchangeBalance
            {
                Free = 2
            },
            Info = new ExchangeMarketInfo
            {
                BaseAsset = sellAsset.Symbol,
                QuoteAsset = hedgeInstruction.TargetAssetSymbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindDirectMarketsAsync(default, default)
            .ReturnsForAnyArgs(new List<DirectHedgeExchangeMarket> { market });
        _pricesService.GetConvertPriceAsync(default, default).ReturnsForAnyArgs(1);
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage,
            _exchangesAnalyzer, _hedgeSettingsStorage, _pricesService);

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

        var market = new DirectHedgeExchangeMarket
        {
            Balance = new ExchangeBalance
            {
                Free = 100
            },
            Info = new ExchangeMarketInfo
            {
                BaseAsset = sellAsset.Symbol,
                QuoteAsset = hedgeInstruction.TargetAssetSymbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindDirectMarketsAsync(default, default)
            .ReturnsForAnyArgs(new List<DirectHedgeExchangeMarket> { market });
        _pricesService.GetConvertPriceAsync(default, default).ReturnsForAnyArgs(1);
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage,
            _exchangesAnalyzer, _hedgeSettingsStorage, _pricesService);

        // act
        await service.HedgeAsync(hedgeInstruction);

        // assert
        await _externalMarket.Received().MarketTrade(
            Arg.Is<MarketTradeRequest>(request =>
                request.Side == OrderSide.Sell &&
                Convert.ToDecimal(request.Volume) == market.Balance.Free));
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

        var market = new DirectHedgeExchangeMarket
        {
            Balance = new ExchangeBalance
            {
                Free = 100
            },
            Info = new ExchangeMarketInfo
            {
                BaseAsset = hedgeInstruction.TargetAssetSymbol,
                QuoteAsset = sellAsset.Symbol,
                MinVolume = 1,
            }
        };
        _exchangesAnalyzer
            .FindDirectMarketsAsync(default, default)
            .ReturnsForAnyArgs(new List<DirectHedgeExchangeMarket> { market });
        _pricesService.GetConvertPriceAsync(default, default).ReturnsForAnyArgs(1);
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade());
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage,
            _exchangesAnalyzer, _hedgeSettingsStorage, _pricesService);

        // act
        await service.HedgeAsync(hedgeInstruction);

        // assert
        await _externalMarket.Received().MarketTrade(
            Arg.Is<MarketTradeRequest>(request =>
                request.Side == OrderSide.Buy &&
                Convert.ToDecimal(request.Volume) == market.Balance.Free));
    }

    [Test]
    public async Task Hedge_NoDirectMarket_MakesTradeOnIndirectMarket()
    {
        // arrange
        var transitAsset = new HedgePairAsset
        {
            Symbol = "USD"
        };
        ;
        var pairAsset = new HedgePairAsset
        {
            Symbol = "BTC"
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 110,
            TargetAssetSymbol = "XRP",
            PairAssets = new List<HedgePairAsset> { pairAsset },
        };

        var market = new IndirectHedgeExchangeMarket
        {
            TransitAssetSymbol = transitAsset.Symbol,
            TransitMarketInfo = new ExchangeMarketInfo
            {
                BaseAsset = transitAsset.Symbol,
                QuoteAsset = pairAsset.Symbol,
                MinVolume = 1,
            },
            TargetMarketInfo = new ExchangeMarketInfo
            {
                BaseAsset = hedgeInstruction.TargetAssetSymbol,
                QuoteAsset = pairAsset.Symbol,
                MinVolume = 1,
            },
            TransitPairAssetSymbol = pairAsset.Symbol
        };
        _exchangesAnalyzer
            .FindIndirectMarketsAsync(default, default, default, default)
            .ReturnsForAnyArgs(new List<IndirectHedgeExchangeMarket> { market });
        _pricesService.GetConvertPriceAsync(default, default).ReturnsForAnyArgs(1);
        _externalMarket.MarketTrade(default).ReturnsForAnyArgs(new ExchangeTrade
        {
            Volume = Convert.ToDouble(hedgeInstruction.TargetVolume),
            OppositeVolume = Convert.ToDouble(hedgeInstruction.TargetVolume),
        });
        _externalMarket.GetBalancesAsync(default).ReturnsForAnyArgs(new GetBalancesResponse
        {
            Balances = new List<ExchangeBalance>
            {
                new ()
                {
                    Symbol = transitAsset.Symbol,
                    Free = 0
                },
                new ()
                {
                    Symbol = pairAsset.Symbol,
                    Free = 100
                }
            }
        });
        _hedgeSettingsStorage.GetAsync().ReturnsForAnyArgs(new HedgeSettings
            { IndirectMarketTransitAssets = new List<string> { "USD" }, EnabledExchanges = new List<string> {"exchange"}});
        var service = new HedgeService(_logger, _externalMarket, _hedgeOperationsStorage,
            _exchangesAnalyzer, _hedgeSettingsStorage, _pricesService);

        // act
        await service.HedgeAsync(hedgeInstruction);

        // assert
        await _externalMarket.Received(2).MarketTrade(
            Arg.Any<MarketTradeRequest>());
    }
}