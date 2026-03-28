#nullable disable
namespace SantiBot.Db.Models;

public class CountingConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int CurrentCount { get; set; }
    public ulong LastCountUserId { get; set; }
    public bool Enabled { get; set; }
}
