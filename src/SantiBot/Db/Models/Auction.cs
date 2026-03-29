#nullable disable
namespace SantiBot.Db.Models;

public class Auction : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong SellerId { get; set; }
    public string SellerName { get; set; }
    public string ItemDescription { get; set; }
    public long StartPrice { get; set; }
    public long CurrentBid { get; set; }
    public ulong HighestBidderId { get; set; }
    public string HighestBidderName { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; }
}
