using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Service.IndexPrices.Client;
using Service.IndexPrices.Domain.Models;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Services.Strategies;
using Service.Liquidity.Monitoring.Domain.Models.Checks;
using Service.Liquidity.Monitoring.Domain.Models.Rules;
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
                        NetBalanceInUsd = -40,
                        NetBalance = 40,
                        DailyVelocityRiskInUsd = -1
                    }
                },
                {
                    "BTC", new Portfolio.Asset
                    {
                        Symbol = "BTC",
                        NetBalanceInUsd = -60,
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
        var rule = new MonitoringRule
        {
            Checks = checks
        };
        var strategy = new ClosePositionMaxVelocityHedgeStrategy();
        
        var instruction = strategy.CalculateHedgeInstruction(portfolio, rule, 30m);
        
        Assert.IsNotNull(instruction);
        Assert.AreEqual(instruction.TargetVolume, 30);
        Assert.AreEqual(instruction.TargetAssetSymbol, "BTC");
        Assert.IsNotEmpty(instruction.PairAssets);
        Assert.AreEqual(instruction.PairAssets.First().Symbol, "BUSD2");
        Assert.AreEqual(instruction.PairAssets.Last().Symbol, "BUSD");

    }
}