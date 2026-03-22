#nullable disable
namespace SantiBot.Db.Models;

public class FlagTranslateChannel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}