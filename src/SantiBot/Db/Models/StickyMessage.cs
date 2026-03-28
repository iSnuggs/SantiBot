#nullable disable
namespace SantiBot.Db.Models;

public class StickyMessage : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Content { get; set; } = "";
    public ulong? CurrentMessageId { get; set; }
    public bool Enabled { get; set; } = true;
}
