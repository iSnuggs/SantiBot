#nullable disable
namespace SantiBot.Db.Models;

public class ImageOnlyChannel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public OnlyChannelType Type { get; set; }
}

public enum OnlyChannelType
{
    Image,
    Link
}