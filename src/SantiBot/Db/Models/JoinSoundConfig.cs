#nullable disable
namespace SantiBot.Db.Models;

public class JoinSoundConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public string DefaultJoinUrl { get; set; } = ""; // URL to audio file
    public int MaxDurationSeconds { get; set; } = 5;
}
