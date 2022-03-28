using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Hedger.Domain.Services;
using Service.Liquidity.TradingPortfolio.Domain.Models;

namespace Service.Liquidity.Hedger.Tests;

public class PortfolioAnalyzerTests
{
    private ILogger<PortfolioAnalyzer> _logger;
    private IHedgeOperationsStorage _hedgeOperationsStorage;
    private IHedgeStrategiesFactory _hedgeStrategiesFactory;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<PortfolioAnalyzer>>();
        _hedgeOperationsStorage = Substitute.For<IHedgeOperationsStorage>();
        _hedgeStrategiesFactory = Substitute.For<IHedgeStrategiesFactory>();
    }

    [Test]
    public async Task TimeToHedge_PortfolioWithoutOperationId_ReturnsTrue()
    {
        // arrange
        var portfolio = new Portfolio
        {
            HedgeOperationId = null,
        };
        var service = new PortfolioAnalyzer(_logger, _hedgeStrategiesFactory, _hedgeOperationsStorage);
        
        // act
        var result = await service.TimeToHedge(portfolio);

        // assert
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task TimeToHedge_NoLastOperationAndPortfolioWithOperationId_ReturnsTrue()
    {
        // arrange
        var portfolio = new Portfolio
        {
            HedgeOperationId = "test",
        };
        var service = new PortfolioAnalyzer(_logger, _hedgeStrategiesFactory, _hedgeOperationsStorage);
        
        // act
        var result = await service.TimeToHedge(portfolio);

        // assert
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task TimeToHedge_PortfolioWithOperationIdDoNotEquals_ReturnsFalse()
    {
        // arrange
        var portfolio = new Portfolio
        {
            HedgeOperationId = "test",
        };
        _hedgeOperationsStorage.GetLastAsync().ReturnsForAnyArgs(new HedgeOperation()
        {
            Id = "test2"
        });
        var service = new PortfolioAnalyzer(_logger, _hedgeStrategiesFactory, _hedgeOperationsStorage);
        
        // act
        var result = await service.TimeToHedge(portfolio);

        // assert
        result.Should().BeFalse();
    }
    
    [Test]
    public async Task TimeToHedge_PortfolioWithOperationIdEquals_ReturnsTrue()
    {
        // arrange
        var operation = new HedgeOperation
        {
            Id = "test"
        };
        var portfolio = new Portfolio
        {
            HedgeOperationId = operation.Id,
        };
        _hedgeOperationsStorage.GetLastAsync().ReturnsForAnyArgs(operation);
        var service = new PortfolioAnalyzer(_logger, _hedgeStrategiesFactory, _hedgeOperationsStorage);
        
        // act
        var result = await service.TimeToHedge(portfolio);

        // assert
        result.Should().BeTrue();
    }
}