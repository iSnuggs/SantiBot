#nullable disable
namespace SantiBot.Db.Models;

public class UserBusiness : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong OwnerId { get; set; }
    public string Name { get; set; }
    public string BusinessType { get; set; } // Restaurant, Shop, Farm, Factory
    public long Revenue { get; set; }
    public DateTime LastCollected { get; set; }
}

public class BusinessEmployee : DbEntity
{
    public ulong GuildId { get; set; }
    public int BusinessId { get; set; }
    public ulong UserId { get; set; }
    public DateTime HiredAt { get; set; }
    public DateTime LastWorked { get; set; }
}
