#nullable disable
namespace SantiBot.Db.Models;

public class XpChallengeProgress : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int ChallengeId { get; set; }
    public int CurrentAmount { get; set; }
    public bool Completed { get; set; }
    public bool Claimed { get; set; }
}
