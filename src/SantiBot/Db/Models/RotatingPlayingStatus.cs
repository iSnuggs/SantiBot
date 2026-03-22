#nullable disable
namespace SantiBot.Db.Models;

public class RotatingPlayingStatus : DbEntity
{
    public string Status { get; set; }
    public DbActivityType Type { get; set; }
}