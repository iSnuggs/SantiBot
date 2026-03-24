#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// A per-channel rule that automatically deletes messages after a delay.
/// Optionally filter which messages get deleted.
/// </summary>
public class AutoDeleteRule : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public bool Enabled { get; set; } = true;

    /// <summary>Delay in seconds before deleting the message. 0 = instant.</summary>
    public int DelaySeconds { get; set; } = 5;

    /// <summary>
    /// If true, only delete messages matching the filter. If false, delete all messages.
    /// </summary>
    public bool UseFilter { get; set; }

    /// <summary>
    /// Filter type: "bots" (only bot messages), "humans" (only human messages),
    /// "contains:text" (messages containing text), "attachments" (messages with files),
    /// "links" (messages with URLs), or empty for all messages.
    /// </summary>
    public string Filter { get; set; }

    /// <summary>If true, pinned messages are never deleted.</summary>
    public bool IgnorePinned { get; set; } = true;
}

public class AutoDeleteRuleEntityConfiguration : IEntityTypeConfiguration<AutoDeleteRule>
{
    public void Configure(EntityTypeBuilder<AutoDeleteRule> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.ChannelId });
    }
}
