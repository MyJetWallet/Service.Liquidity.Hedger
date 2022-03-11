using System;

namespace Service.Liquidity.Hedger.Domain.Models
{
    public class HedgeOperationId
    {
        public long Value { get; private set; }

        public HedgeOperationId()
        {
            Value = DateTime.UtcNow.Ticks;
        }

        public void Increase()
        {
            Value = DateTime.UtcNow.Ticks;
        }
    }
}