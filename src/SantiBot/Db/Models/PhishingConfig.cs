#nullable disable
namespace SantiBot.Db.Models;

public class PhishingConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }

    /// <summary>
    /// Action on phishing detection: Delete, Warn, Mute, Ban
    /// </summary>
    public string Action { get; set; } = "Delete";

    public ulong? LogChannelId { get; set; }
}
