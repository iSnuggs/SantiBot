#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

public enum TicketStatus
{
    Open,
    Claimed,    // A mod has claimed the ticket
    Closed,
}

/// <summary>
/// Per-guild ticket system configuration.
/// </summary>
public class TicketConfig : DbEntity
{
    public ulong GuildId { get; set; }

    /// <summary>Whether the ticket system is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Category where ticket channels are created.</summary>
    public ulong? CategoryId { get; set; }

    /// <summary>Channel where the ticket log/transcript is posted on close.</summary>
    public ulong? LogChannelId { get; set; }

    /// <summary>Role that can view and manage tickets.</summary>
    public ulong? SupportRoleId { get; set; }

    /// <summary>Maximum open tickets per user. 0 = unlimited.</summary>
    public int MaxTicketsPerUser { get; set; } = 1;

    /// <summary>Custom welcome message when a ticket is created. Variables: {user}, {server}, {ticket}</summary>
    public string WelcomeMessage { get; set; } = "Hey {user}, thanks for creating a ticket! A staff member will be with you shortly.";

    /// <summary>Channel where the "Create Ticket" button panel is posted.</summary>
    public ulong? PanelChannelId { get; set; }

    /// <summary>Message ID of the panel with the Create Ticket button.</summary>
    public ulong? PanelMessageId { get; set; }
}

/// <summary>
/// An individual support ticket — each one creates a private channel.
/// </summary>
public class Ticket : DbEntity
{
    public ulong GuildId { get; set; }

    /// <summary>Auto-incrementing ticket number within the guild.</summary>
    public int TicketNumber { get; set; }

    /// <summary>The user who opened the ticket.</summary>
    public ulong CreatorUserId { get; set; }

    /// <summary>The mod who claimed the ticket (if any).</summary>
    public ulong? ClaimedByUserId { get; set; }

    /// <summary>The channel created for this ticket.</summary>
    public ulong ChannelId { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    /// <summary>Optional topic/subject for the ticket.</summary>
    public string Topic { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    /// <summary>Who closed the ticket.</summary>
    public ulong? ClosedByUserId { get; set; }
}

// ── Entity Framework Configurations ──

public class TicketConfigEntityConfiguration : IEntityTypeConfiguration<TicketConfig>
{
    public void Configure(EntityTypeBuilder<TicketConfig> builder)
    {
        builder.HasIndex(x => x.GuildId).IsUnique();
    }
}

public class TicketEntityConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.TicketNumber }).IsUnique();
        builder.HasIndex(x => x.ChannelId);
    }
}
