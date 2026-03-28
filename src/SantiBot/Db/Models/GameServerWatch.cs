#nullable disable
namespace SantiBot.Db.Models;

public class GameServerWatch : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong? StatusMessageId { get; set; }
    public string GameType { get; set; } = "minecraft";
    public string ServerAddress { get; set; } = "";
    public int ServerPort { get; set; }
    public bool AutoUpdate { get; set; } = true;
}
