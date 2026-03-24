#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// A one-time or recurring scheduled message to be sent at a specific time.
/// Extends the existing Repeater system with Dyno-style timed messages.
/// </summary>
public class ScheduledMessage : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

    /// <summary>The message text to send. Supports embed JSON.</summary>
    public string Content { get; set; }

    /// <summary>Whether this is a one-time message or recurring.</summary>
    public bool IsRecurring { get; set; }

    /// <summary>When to send the message (UTC).</summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>Interval for recurring messages. Null for one-time.</summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>When this was last sent (for recurring messages).</summary>
    public DateTime? LastSentAt { get; set; }

    /// <summary>Whether this message has been sent (for one-time) or is active (for recurring).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Who created this scheduled message.</summary>
    public ulong CreatorUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ScheduledMessageEntityConfiguration : IEntityTypeConfiguration<ScheduledMessage>
{
    public void Configure(EntityTypeBuilder<ScheduledMessage> builder)
    {
        builder.HasIndex(x => x.GuildId);
        builder.HasIndex(x => new { x.IsActive, x.ScheduledAt });
    }
}
