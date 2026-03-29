#nullable disable
namespace SantiBot.Db.Models;

public class UserLoan : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long Principal { get; set; }
    public long AmountOwed { get; set; }
    public int CreditScore { get; set; }
    public DateTime TakenAt { get; set; }
    public DateTime LastInterestApplied { get; set; }
    public bool IsActive { get; set; }
}

public class LoanHistory : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long Amount { get; set; }
    public bool RepaidOnTime { get; set; }
}
