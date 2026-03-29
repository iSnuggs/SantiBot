#nullable disable
namespace SantiBot.Db.Models;

public class OwnedBackground : DbEntity
{
    public ulong UserId { get; set; }
    public string BackgroundId { get; set; }
}
