#nullable disable
namespace SantiBot.Db.Models;

public class ShopListing : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong SellerUserId { get; set; }
    public string ItemName { get; set; } = "";
    public string Description { get; set; } = "";
    public long Price { get; set; }
    public int Stock { get; set; } = -1; // -1 = unlimited
    public bool Active { get; set; } = true;
}
