namespace Service.Liquidity.Hedger.Domain.Models;

public enum HedgeInstructionStatus
{
    Pending = 0,
    InProgress = 1,
    Processed = 2,
    Confirmed = 3,
}