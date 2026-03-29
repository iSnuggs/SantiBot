#nullable disable
namespace SantiBot.Db.Models;

public class LevelUpMessage : DbEntity
{
    public ulong GuildId { get; set; }
    public string MessageTemplate { get; set; }
    public ulong ChannelId { get; set; }
    public bool Enabled { get; set; }
}
