#nullable disable
namespace SantiBot.Db.Models;

public class DropdownRolePanel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public string Title { get; set; }
}

public class DropdownRoleOption : DbEntity
{
    public int PanelId { get; set; }
    public ulong GuildId { get; set; }
    public ulong MessageId { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public ulong RoleId { get; set; }
    public string Emote { get; set; }
}
