namespace Service.Liquidity.Hedger.Domain.Models;

public enum HedgeInstructionStatus
{
    Pending = 0,
    Started = 1,
    Finished = 2,
    Confirmed = 3,
}