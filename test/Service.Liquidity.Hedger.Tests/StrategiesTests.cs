using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Hedger.Domain.Services.Strategies;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Tests;

public class StrategiesTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ShouldCalculateClosePositionMaxVelocity()
    {
        var portfolio = new Portfolio
        {
            Assets = new Dictionary<string, Portfolio.Asset>
            {
                {
                    "ETH2", new Portfolio.Asset
                    {
                        Symbol = "ETH2",
                        NetBalanceInUsd = -1,
                        NetBalance = 40,
                        DailyVelocityRiskInUsd = 3
                    }
                },
                {
                    "ETH", new Portfolio.Asset
                    {
                        Symbol = "ETH",
                        NetBalanceInUsd = -1,
                        NetBalance = 40,
                        DailyVelocityRiskInUsd = -1
                    }
                },
                {
                    "BTC", new Portfolio.Asset
                    {
                        Symbol = "BTC",
                        NetBalanceInUsd = -1,
                        NetBalance = 60,
                        DailyVelocityRiskInUsd = -2
                    }
                },
                {
                    "BUSD", new Portfolio.Asset
                    {
                        Symbol = "BUSD",
                        NetBalanceInUsd = 1,
                        NetBalance = 40,
                        DailyVelocityRiskInUsd = -2
                    }
                },
                {
                    "BUSD2", new Portfolio.Asset
                    {
                        Symbol = "BUSD2",
                        NetBalanceInUsd = 1,
                        NetBalance = 40,
                        DailyVelocityRiskInUsd = -3
                    }
                }
            }
        };
        var checks = new List<PortfolioCheck>
        {
            new ()
            {
                CurrentState = new PortfolioCheckState
                {
                    IsActive = true
                },
                AssetSymbols = new List<string>
                {
                    "BTC", "ETH"
                }
            },
            new ()
            {
                CurrentState = new PortfolioCheckState
                {
                    IsActive = false
                },
                AssetSymbols = new List<string>
                {
                    "BTC2", "ETH2"
                }
            }
        };
        var strParams = new HedgeStrategyParams
        {
            AmountPercent = 30m,
        };
        var strategy = new ClosePositionMaxVelocityHedgeStrategy();
        
        var instruction = strategy.CalculateHedgeInstruction(portfolio, checks, strParams);
        
        Assert.IsNotNull(instruction);
        Assert.AreEqual(instruction.BuyVolume, 30);
        Assert.AreEqual(instruction.BuyAssetSymbol, "BTC");
        Assert.IsNotEmpty(instruction.SellAssets);
        Assert.AreEqual(instruction.SellAssets.First().Symbol, "BUSD2");
        Assert.AreEqual(instruction.SellAssets.Last().Symbol, "BUSD");

    }
}