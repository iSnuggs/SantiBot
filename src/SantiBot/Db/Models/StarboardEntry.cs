#nullable disable
namespace SantiBot.Db.Models;

public class StarboardEntry : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong AuthorId { get; set; }
    public ulong StarboardMessageId { get; set; }
    public int StarCount { get; set; }
}

public class StarboardSettings : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong StarboardChannelId { get; set; }
    public int StarThreshold { get; set; } = 3;
    public string StarEmoji { get; set; } = "⭐";
    public bool AllowSelfStar { get; set; } = false;
}
