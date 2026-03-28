#nullable disable
namespace SantiBot.Db.Models;

public class CraftingInventory : DbEntity
{
    public ulong UserId { get; set; }
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; }
}
