#nullable disable
namespace SantiBot.Db.Models;

public class AutoPurgeConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

    /// <summary>
    /// How often to purge, in hours
    /// </summary>
    public int IntervalHours { get; set; }

    /// <summary>
    /// Delete messages older than this many hours
    /// </summary>
    public int MaxMessageAgeHours { get; set; }

    public bool IsActive { get; set; } = true;
}
