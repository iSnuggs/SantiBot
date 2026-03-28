#nullable disable
namespace SantiBot.Db.Models;

public class BanAppeal : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Reason { get; set; } = "";
    public string AppealText { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public ulong? ReviewedByUserId { get; set; }
}
