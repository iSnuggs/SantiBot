#nullable disable
namespace SantiBot.Db.Models;

public class ProfileBackground : DbEntity
{
    public string BackgroundId { get; set; }
    public string Name { get; set; }
    public string HexColor { get; set; }
    public long Price { get; set; }
}
