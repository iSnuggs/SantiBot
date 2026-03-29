#nullable disable
namespace SantiBot.Db.Models;

public class XpChallenge : DbEntity
{
    public ulong GuildId { get; set; }
    public string ChallengeType { get; set; } // messages, reactions, voice, bets
    public string Description { get; set; }
    public int TargetAmount { get; set; }
    public long BonusXp { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
