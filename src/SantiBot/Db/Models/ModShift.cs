#nullable disable
namespace SantiBot.Db.Models;

public class ModShift : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
}
