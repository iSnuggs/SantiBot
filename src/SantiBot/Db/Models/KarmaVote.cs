#nullable disable
namespace SantiBot.Db.Models;

public class KarmaVote : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong VoterId { get; set; }
    public ulong TargetUserId { get; set; }
    public ulong MessageId { get; set; }
    public bool IsUpvote { get; set; }
}
