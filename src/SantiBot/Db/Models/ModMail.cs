#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

public enum ModMailThreadStatus
{
    Open,
    Closed,
}

/// <summary>
/// Per-guild mod mail configuration.
/// </summary>
public class ModMailConfig : DbEntity
{
    public ulong GuildId { get; set; }

    /// <summary>Whether mod mail is enabled for this guild.</summary>
    public bool Enabled { get; set; }

    /// <summary>Category where mod mail thread channels are created.</summary>
    public ulong? CategoryId { get; set; }

    /// <summary>Channel where closed thread transcripts are logged.</summary>
    public ulong? LogChannelId { get; set; }

    /// <summary>Role that can manage mod mail threads.</summary>
    public ulong? StaffRoleId { get; set; }

    /// <summary>Initial DM message sent to user when their thread is opened.</summary>
    public string OpenMessage { get; set; } = "Your message has been sent to the staff team of **{server}**. They will reply here shortly.";

    /// <summary>DM message sent to user when their thread is closed.</summary>
    public string CloseMessage { get; set; } = "Your mod mail thread in **{server}** has been closed. Feel free to DM again if you need further help.";
}

/// <summary>
/// A mod mail thread — links a user's DM conversation to a guild channel.
/// </summary>
public class ModMailThread : DbEntity
{
    public ulong GuildId { get; set; }

    /// <summary>The user who initiated the thread via DM.</summary>
    public ulong UserId { get; set; }

    /// <summary>The guild channel created for this thread.</summary>
    public ulong ChannelId { get; set; }

    public ModMailThreadStatus Status { get; set; } = ModMailThreadStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    /// <summary>Who closed the thread (staff user ID).</summary>
    public ulong? ClosedByUserId { get; set; }

    /// <summary>Total messages exchanged in this thread.</summary>
    public int MessageCount { get; set; }
}

/// <summary>
/// Archived message within a mod mail thread (for transcripts).
/// </summary>
public class ModMailMessage : DbEntity
{
    public int ThreadId { get; set; }

    /// <summary>Who sent this message (user or staff).</summary>
    public ulong AuthorId { get; set; }

    /// <summary>True if sent by staff, false if sent by the user.</summary>
    public bool IsStaff { get; set; }

    /// <summary>The message content.</summary>
    public string Content { get; set; }

    /// <summary>Attachment URLs (pipe-separated).</summary>
    public string Attachments { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Users blocked from using mod mail in a guild.
/// </summary>
public class ModMailBlock : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    /// <summary>Reason for the block.</summary>
    public string Reason { get; set; }

    public ulong BlockedByUserId { get; set; }
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
}

// ── Entity Framework Configurations ──

public class ModMailConfigEntityConfiguration : IEntityTypeConfiguration<ModMailConfig>
{
    public void Configure(EntityTypeBuilder<ModMailConfig> builder)
    {
        builder.HasIndex(x => x.GuildId).IsUnique();
    }
}

public class ModMailThreadEntityConfiguration : IEntityTypeConfiguration<ModMailThread>
{
    public void Configure(EntityTypeBuilder<ModMailThread> builder)
    {
        builder.HasIndex(x => x.ChannelId);
        builder.HasIndex(x => new { x.GuildId, x.UserId, x.Status });
    }
}

public class ModMailMessageEntityConfiguration : IEntityTypeConfiguration<ModMailMessage>
{
    public void Configure(EntityTypeBuilder<ModMailMessage> builder)
    {
        builder.HasIndex(x => x.ThreadId);
    }
}

public class ModMailBlockEntityConfiguration : IEntityTypeConfiguration<ModMailBlock>
{
    public void Configure(EntityTypeBuilder<ModMailBlock> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.UserId }).IsUnique();
    }
}
