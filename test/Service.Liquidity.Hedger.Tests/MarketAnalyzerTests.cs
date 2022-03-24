using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using NSubstitute;
using NUnit.Framework;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Hedger.Domain.Services;

namespace Service.Liquidity.Hedger.Tests;

public class MarketAnalyzerTests
{
    private ILogger<ExchangesAnalyzer> _logger;
    private IExternalMarket _externalMarket;
    
    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExchangesAnalyzer>>();
        _externalMarket = Substitute.For<IExternalMarket>();
    }

    [Test]
    public async Task FindPossibleMarkets_HasOneMarket_FindsMarketWithBaseTargetAsset()
    {
        // arrange
        var btcSellAsset = new HedgeSellAsset
        {
            Symbol = "BTC",
            Weight = -2
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 4,
            TargetAssetSymbol = "XRP",
            SellAssets = new List<HedgeSellAsset>
            {
                btcSellAsset
            }
        };
        _externalMarket.GetBalancesAsync(default).ReturnsForAnyArgs(new GetBalancesResponse
        {
            Balances = new List<ExchangeBalance>
            {
                new ()
                {
                    Symbol = btcSellAsset.Symbol,
                    Free = 10
                },
            }
        });
        _externalMarket.GetMarketInfoListAsync(default).ReturnsForAnyArgs(new GetMarketInfoListResponse
        {
            Infos = new List<ExchangeMarketInfo>
            {

                new ()
                {
                    MinVolume = 1,
                    BaseAsset = hedgeInstruction.TargetAssetSymbol,
                    QuoteAsset = btcSellAsset.Symbol,
                }
            }
        });
        var analyzer = new ExchangesAnalyzer(_logger, _externalMarket);
        
        // act
        var markets = await analyzer.FindPossibleMarketsAsync(hedgeInstruction);
        
        // assert
        markets.Should().NotBeNull();
        markets.Should().NotBeEmpty();
        markets.First().Weight.Should().Be(btcSellAsset.Weight);
        markets.First().AssetExchangeBalance.Symbol.Should().Be(btcSellAsset.Symbol);
        markets.First().ExchangeMarketInfo.QuoteAsset.Should().Be(btcSellAsset.Symbol);
        markets.First().ExchangeMarketInfo.BaseAsset.Should().Be(hedgeInstruction.TargetAssetSymbol);
    }
    
    [Test]
    public async Task FindPossibleMarkets_HasSeveralMarkets_FiltersOutWrongMarket()
    {
        // arrange
        var btcSellAsset = new HedgeSellAsset
        {
            Symbol = "BTC",
            Weight = -2
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 4,
            TargetAssetSymbol = "XRP",
            SellAssets = new List<HedgeSellAsset>
            {
                btcSellAsset
            }
        };
        _externalMarket.GetBalancesAsync(default).ReturnsForAnyArgs(new GetBalancesResponse
        {
            Balances = new List<ExchangeBalance>
            {
                new ()
                {
                    Symbol = btcSellAsset.Symbol,
                    Free = 10
                },
                new ()
                {
                    Symbol = "fsd",
                    Free = 10
                },
            }
        });
        _externalMarket.GetMarketInfoListAsync(default).ReturnsForAnyArgs(new GetMarketInfoListResponse
        {
            Infos = new List<ExchangeMarketInfo>
            {
                new ()
                {
                    MinVolume = 1,
                    BaseAsset = hedgeInstruction.TargetAssetSymbol,
                    QuoteAsset = btcSellAsset.Symbol,
                },
                new ()
                {
                    MinVolume = 1,
                    BaseAsset = "fsd",
                    QuoteAsset = "fsdf",
                },
            }
        });
        var analyzer = new ExchangesAnalyzer(_logger, _externalMarket);
        
        // act
        var markets = await analyzer.FindPossibleMarketsAsync(hedgeInstruction);
        
        // assert
        markets.Count.Should().Be(1);        
    }

    [Test]
    public async Task FindPossibleMarkets_HasOneMarket_FindsMarketWithQuoteTargetAsset()
    {
        // arrange
        var btcSellAsset = new HedgeSellAsset
        {
            Symbol = "BTC",
            Weight = -2
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 4,
            TargetAssetSymbol = "XRP",
            SellAssets = new List<HedgeSellAsset>
            {
                btcSellAsset
            }
        };
        _externalMarket.GetBalancesAsync(default).ReturnsForAnyArgs(new GetBalancesResponse
        {
            Balances = new List<ExchangeBalance>
            {
                new ()
                {
                    Symbol = btcSellAsset.Symbol,
                    Free = 10
                },
            }
        });
        _externalMarket.GetMarketInfoListAsync(default).ReturnsForAnyArgs(new GetMarketInfoListResponse
        {
            Infos = new List<ExchangeMarketInfo>
            {

                new ()
                {
                    MinVolume = 1,
                    BaseAsset = btcSellAsset.Symbol,
                    QuoteAsset = hedgeInstruction.TargetAssetSymbol,
                }
            }
        });
        var analyzer = new ExchangesAnalyzer(_logger, _externalMarket);
        
        // act
        var markets = await analyzer.FindPossibleMarketsAsync(hedgeInstruction);
        
        // assert
        markets.Should().NotBeNull();
        markets.Should().NotBeEmpty();
        markets.First().Weight.Should().Be(btcSellAsset.Weight);
        markets.First().AssetExchangeBalance.Symbol.Should().Be(btcSellAsset.Symbol);
        markets.First().ExchangeMarketInfo.BaseAsset.Should().Be(btcSellAsset.Symbol);
        markets.First().ExchangeMarketInfo.QuoteAsset.Should().Be(hedgeInstruction.TargetAssetSymbol);
    }
    
    [Test]
    public async Task FindPossibleMarkets_HasMarketWithZeroBalance_FiltersMarketWithZeroBalance()
    {
        // arrange
        var btcSellAsset = new HedgeSellAsset
        {
            Symbol = "BTC",
            Weight = -2
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 4,
            TargetAssetSymbol = "XRP",
            SellAssets = new List<HedgeSellAsset>
            {
                btcSellAsset
            }
        };
        _externalMarket.GetBalancesAsync(default).ReturnsForAnyArgs(new GetBalancesResponse
        {
            Balances = new List<ExchangeBalance>
            {
                new ()
                {
                    Symbol = btcSellAsset.Symbol,
                    Free = 0
                },
            }
        });
        _externalMarket.GetMarketInfoListAsync(default).ReturnsForAnyArgs(new GetMarketInfoListResponse
        {
            Infos = new List<ExchangeMarketInfo>
            {

                new ()
                {
                    MinVolume = 1,
                    BaseAsset = btcSellAsset.Symbol,
                    QuoteAsset = hedgeInstruction.TargetAssetSymbol,
                }
            }
        });
        var analyzer = new ExchangesAnalyzer(_logger, _externalMarket);
        
        // act
        var markets = await analyzer.FindPossibleMarketsAsync(hedgeInstruction);
        
        // assert
        markets.Should().BeEmpty();
    }
    
    [Test]
    public async Task FindPossibleMarkets_HasMarketWithMinVolumeLessThenTarget_FiltersMarketMarketWithMinVolumeLessThenTarget()
    {
        // arrange
        var btcSellAsset = new HedgeSellAsset
        {
            Symbol = "BTC",
            Weight = -2
        };
        var hedgeInstruction = new HedgeInstruction
        {
            TargetVolume = 0.5m,
            TargetAssetSymbol = "XRP",
            SellAssets = new List<HedgeSellAsset>
            {
                btcSellAsset
            }
        };
        _externalMarket.GetBalancesAsync(default).ReturnsForAnyArgs(new GetBalancesResponse
        {
            Balances = new List<ExchangeBalance>
            {
                new ()
                {
                    Symbol = btcSellAsset.Symbol,
                    Free = 1
                },
            }
        });
        _externalMarket.GetMarketInfoListAsync(default).ReturnsForAnyArgs(new GetMarketInfoListResponse
        {
            Infos = new List<ExchangeMarketInfo>
            {

                new ()
                {
                    MinVolume = 1,
                    BaseAsset = btcSellAsset.Symbol,
                    QuoteAsset = hedgeInstruction.TargetAssetSymbol,
                }
            }
        });
        var analyzer = new ExchangesAnalyzer(_logger, _externalMarket);
        
        // act
        var markets = await analyzer.FindPossibleMarketsAsync(hedgeInstruction);
        
        // assert
        markets.Should().BeEmpty();
    }

}