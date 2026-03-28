#nullable disable
namespace SantiBot.Db.Models;

public class TrackedInvite : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong InviterUserId { get; set; }
    public ulong InvitedUserId { get; set; }
    public string InviteCode { get; set; } = "";
    public DateTime JoinedAt { get; set; }
}
