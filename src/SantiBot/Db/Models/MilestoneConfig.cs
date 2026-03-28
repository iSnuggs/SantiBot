namespace SantiBot.Db.Models;

public class MilestoneConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong? ChannelId { get; set; }
    public bool Enabled { get; set; }
    public int LastMilestone { get; set; }
}
