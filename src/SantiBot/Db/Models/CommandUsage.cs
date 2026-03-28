namespace SantiBot.Db.Models;

public class CommandUsage : DbEntity
{
    public ulong GuildId { get; set; }
    public string CommandName { get; set; } = "";
    public int UsageCount { get; set; }
    public DateTime Date { get; set; } // one row per command per day
}
