#nullable disable
namespace SantiBot.Db.Models;

public class EvidenceItem : DbEntity
{
    public ulong GuildId { get; set; }
    public int CaseId { get; set; }
    public string Url { get; set; }
    public string Note { get; set; }
    public ulong AddedByUserId { get; set; }
}
