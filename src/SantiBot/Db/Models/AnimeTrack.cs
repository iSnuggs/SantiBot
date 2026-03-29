#nullable disable
namespace SantiBot.Db.Models;

public class AnimeTrack : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int AniListId { get; set; }
    public string Title { get; set; }
    public int LastEpisode { get; set; }
}
