using System;

namespace Service.Liquidity.Hedger.Domain.Extensions;

public static class DecimalExtensions
{
    public static decimal Truncate(this decimal value, int precision)
    {
        var step = (decimal) Math.Pow(10, precision);
        var tmp = Math.Truncate(step * value);

        return tmp / step;
    }
}