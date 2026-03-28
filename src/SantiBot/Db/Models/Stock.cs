#nullable disable
namespace SantiBot.Db.Models;

public class Stock : DbEntity
{
    public string Symbol { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public long PriceInCents { get; set; } = 10000; // $100.00
    public long PreviousPriceInCents { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class StockHolding : DbEntity
{
    public ulong UserId { get; set; }
    public int StockId { get; set; }
    public int Shares { get; set; }
    public long PurchasePriceInCents { get; set; }
}
