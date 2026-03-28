#nullable disable
namespace SantiBot.Db.Models;

public class HeistSession : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong InitiatorUserId { get; set; }
    public long PotAmount { get; set; }
    public string ParticipantIds { get; set; } = ""; // comma-separated user IDs
    public string Status { get; set; } = "Recruiting"; // Recruiting, InProgress, Complete
    public DateTime StartedAt { get; set; }
}
