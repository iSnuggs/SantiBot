#nullable disable
namespace SantiBot.Db.Models;

public class FishingRod : DbEntity
{
    public ulong UserId { get; set; }
    public string RodType { get; set; } = "Basic";
    public int Level { get; set; } = 1;
}
