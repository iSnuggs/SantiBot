#nullable disable
namespace SantiBot.Db.Models;

public class AfkVoiceKickConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }

    /// <summary>
    /// Minutes of voice inactivity before the user is disconnected or moved.
    /// </summary>
    public int IdleMinutes { get; set; } = 30;

    /// <summary>
    /// Users with this role are exempt from AFK kicks.
    /// </summary>
    public ulong? ExemptRoleId { get; set; }

    /// <summary>
    /// If set, idle users are moved here instead of disconnected.
    /// </summary>
    public ulong? AfkChannelId { get; set; }
}
