namespace SantiBot.Db.Models;

public class AntiSpamIgnore : DbEntity
{
    public ulong ChannelId { get; set; }
}