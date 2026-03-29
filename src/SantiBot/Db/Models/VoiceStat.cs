#nullable disable
namespace SantiBot.Db.Models;

public class VoiceStat : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long TotalMinutes { get; set; }
    public ulong FavoriteChannelId { get; set; }
    public long FavoriteChannelMinutes { get; set; }
}
