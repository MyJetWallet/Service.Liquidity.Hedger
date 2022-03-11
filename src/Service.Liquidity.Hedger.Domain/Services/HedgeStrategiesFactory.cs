using System;
using System.Collections.Generic;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Hedger.Domain.Strategies;

namespace Service.Liquidity.Hedger.Domain.Services
{
    public class HedgeStrategiesFactory : IHedgeStrategiesFactory
    {
        private readonly Dictionary<HedgeStrategyType, IHedgeStrategy> _strategies;

        public HedgeStrategiesFactory()
        {
            _strategies = new Dictionary<HedgeStrategyType, IHedgeStrategy>
            {
                { HedgeStrategyType.ClosePositionMaxVelocity, new ClosePositionMaxVelocityHedgeStrategy() },
                { HedgeStrategyType.Return, new ReturnHedgeStrategy() },
                { HedgeStrategyType.None, new NoneHedgeStrategy() },
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