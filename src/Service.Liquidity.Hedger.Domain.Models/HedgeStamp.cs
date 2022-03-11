using System;

namespace Service.Liquidity.Hedger.Domain.Models
{
    public class HedgeStamp
    {
        public long Value { get; set; }

        public HedgeStamp()
        {
            Value = DateTime.UtcNow.Ticks;
        }

        public void Increase()
        {
            Value = DateTime.UtcNow.Ticks;
        }
    }
}