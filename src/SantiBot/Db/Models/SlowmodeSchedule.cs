#nullable disable
namespace SantiBot.Db.Models;

public class SlowmodeSchedule : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int SlowmodeSeconds { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsEnabled { get; set; } = true;
}
