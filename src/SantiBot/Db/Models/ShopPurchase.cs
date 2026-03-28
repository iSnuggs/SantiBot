#nullable disable
namespace SantiBot.Db.Models;

public class ShopPurchase : DbEntity
{
    public int ListingId { get; set; }
    public ulong BuyerUserId { get; set; }
    public DateTime PurchasedAt { get; set; }
}
