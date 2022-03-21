using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Hedger.Domain.Services.Strategies;

namespace Service.Liquidity.Hedger.Domain.Services
{
    public class HedgeStrategiesFactory : IHedgeStrategiesFactory
    {
        private readonly Dictionary<HedgeStrategyType, IHedgeStrategy> _strategies;

        public HedgeStrategiesFactory(
            ILogger<IHedgeStrategy> logger
        )
        {
            _strategies = new Dictionary<HedgeStrategyType, IHedgeStrategy>
            {
                { HedgeStrategyType.ClosePositionMaxVelocity, new ClosePositionMaxVelocityHedgeStrategy(logger) },
                { HedgeStrategyType.Return, new ReturnHedgeStrategy() },
            };
        }

        public IEnumerable<IHedgeStrategy> Get()
        {
            return _strategies.Values;
        }

        public IHedgeStrategy Get(HedgeStrategyType type)
        {
            if (_strategies.TryGetValue(type, out var metric))
            {
                return metric;
            }

            throw new Exception($"Strategy {type.ToString()} Not Found");
        }
    }
}