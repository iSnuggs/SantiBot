namespace SantiBot.Db.Models;

public class AfkUser : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Message { get; set; } = "";
    public DateTime SetAt { get; set; }
}
