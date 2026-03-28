#nullable disable
namespace SantiBot.Db.Models;

public class FishCatch : DbEntity
{
    public ulong UserId { get; set; }
    public string FishName { get; set; } = "";
    public string Rarity { get; set; } = "Common";
    public int Weight { get; set; } // in grams
    public long SellValue { get; set; }
    public DateTime CaughtAt { get; set; }
}
