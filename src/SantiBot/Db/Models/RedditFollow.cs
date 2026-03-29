#nullable disable
namespace SantiBot.Db.Models;

public class RedditFollow : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Subreddit { get; set; }
    public string LastPostId { get; set; }
}
