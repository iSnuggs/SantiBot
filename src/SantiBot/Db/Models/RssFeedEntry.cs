#nullable disable
namespace SantiBot.Db.Models;

public class RssFeedEntry : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Url { get; set; }
    public string LastItemId { get; set; }
}
