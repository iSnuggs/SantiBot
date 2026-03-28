#nullable disable
namespace SantiBot.Db.Models;

public class BumpReminderConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong? PingRoleId { get; set; }
    public bool Enabled { get; set; }
    public DateTime? LastBumpAt { get; set; }
    public int IntervalMinutes { get; set; } = 120;
}
