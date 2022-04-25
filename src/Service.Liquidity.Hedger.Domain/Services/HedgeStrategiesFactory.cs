using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Service.IndexPrices.Client;
using Service.Liquidity.Hedger.Domain.Interfaces;
using Service.Liquidity.Hedger.Domain.Models;
using Service.Liquidity.Hedger.Domain.Services.Strategies;
using IHedgeStrategy = Service.Liquidity.Hedger.Domain.Interfaces.IHedgeStrategy;

namespace Service.Liquidity.Hedger.Domain.Services
{
    public class HedgeStrategiesFactory : IHedgeStrategiesFactory
    {
        private readonly Dictionary<HedgeStrategyType, IHedgeStrategy> _strategies;

        public HedgeStrategiesFactory(
        )
        {
            _strategies = new Dictionary<HedgeStrategyType, IHedgeStrategy>
            {
                {
                    HedgeStrategyType.HedgePositionMaxVelocity,
                    new HedgePositionMaxVelocityStrategy()
                },
                {
                    HedgeStrategyType.HedgeFreeBalance,
                    new HedgeFreeBalanceStrategy()
                },
            };
        }

        public IHedgeStrategy Get(HedgeStrategyType type, Dictionary<string, string> paramValuesByName)
        {
            if (_strategies.TryGetValue(type, out var strategy))
            {
                strategy.ParamValuesByName = paramValuesByName;
                return strategy;
            }

            throw new Exception($"Strategy {type.ToString()} Not Found");
        }
    }
}