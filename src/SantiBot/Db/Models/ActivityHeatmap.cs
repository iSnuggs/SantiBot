#nullable disable
namespace SantiBot.Db.Models;

public class ActivityHeatmap : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public DateTime Date { get; set; }
    public int MessageCount { get; set; }
}
