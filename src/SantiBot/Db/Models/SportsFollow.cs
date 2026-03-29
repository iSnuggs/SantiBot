#nullable disable
namespace SantiBot.Db.Models;

public class SportsFollow : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string League { get; set; }
}
