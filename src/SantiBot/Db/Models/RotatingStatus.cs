namespace SantiBot.Db.Models;

public class RotatingStatus : DbEntity
{
    public string Status { get; set; } = "";
    public int Type { get; set; } // 0=Playing, 1=Streaming, 2=Listening, 3=Watching, 4=Competing
}
