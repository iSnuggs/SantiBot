#nullable disable
namespace SantiBot.Db.Models;

public class IntroConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Template { get; set; }
    public bool Enabled { get; set; }
}
