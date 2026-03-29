#nullable disable
namespace SantiBot.Db.Models;

public class TriviaTournamentEntry : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong UserId { get; set; }
    public string Username { get; set; }
    public int Wins { get; set; }
    public int TotalScore { get; set; }
}
