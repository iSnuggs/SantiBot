#nullable disable
namespace SantiBot.Db.Models;

public class TaxGovernment : DbEntity
{
    public ulong GuildId { get; set; }
    public int TaxRate { get; set; } // percentage 0-50
    public long Treasury { get; set; }
    public ulong ElectedOfficialId { get; set; }
    public DateTime ElectionEndsAt { get; set; }
    public bool ElectionActive { get; set; }
}

public class ElectionVote : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong VoterId { get; set; }
    public ulong CandidateId { get; set; }
}
