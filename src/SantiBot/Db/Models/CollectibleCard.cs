#nullable disable
namespace SantiBot.Db.Models;

public class CollectibleCard : DbEntity
{
    public ulong UserId { get; set; }
    public string CardName { get; set; }
    public string Rarity { get; set; } // Common, Uncommon, Rare, Epic, Legendary
    public string Set { get; set; }
    public int Quantity { get; set; }
}
