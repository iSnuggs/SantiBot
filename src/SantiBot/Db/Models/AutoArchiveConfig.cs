#nullable disable
namespace SantiBot.Db.Models;

public class AutoArchiveConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public int InactiveDays { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class AutoArchiveExclusion : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}
