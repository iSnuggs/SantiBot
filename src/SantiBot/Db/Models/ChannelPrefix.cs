#nullable disable
namespace SantiBot.Db.Models;

public class ChannelPrefix : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Prefix { get; set; }
}
