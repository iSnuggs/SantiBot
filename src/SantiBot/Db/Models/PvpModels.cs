#nullable disable
namespace SantiBot.Db.Models;

public class PvpStats : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    // Record
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }

    // Elo rating (chess-style, starts at 1000)
    public int Elo { get; set; } = 1000;

    // Streaks
    public int WinStreak { get; set; }
    public int BestWinStreak { get; set; }

    // Lifetime combat stats
    public long TotalDamageDealt { get; set; }
    public long TotalDamageReceived { get; set; }

    // Cooldown
    public DateTime? LastDuelAt { get; set; }
}

public class TournamentModel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Name { get; set; } = "";

    // "Registration", "InProgress", "Completed"
    public string Status { get; set; } = "Registration";

    // "SingleElimination", "DoubleElimination", "RoundRobin"
    public string Format { get; set; } = "SingleElimination";

    public int MaxParticipants { get; set; } = 16;
    public long EntryFee { get; set; }
    public long PrizePool { get; set; }
    public ulong CreatedBy { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class TournamentParticipant : DbEntity
{
    public int TournamentId { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public int Seed { get; set; }
    public bool IsEliminated { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
}
