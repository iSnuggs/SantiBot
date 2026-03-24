#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// The type of moderation action taken.
/// </summary>
public enum ModCaseType
{
    Warn,
    Mute,
    TempMute,
    Unmute,
    Kick,
    Ban,
    TempBan,
    Unban,
    Softban,
    TimeOut,
    Note,       // Private mod note — not visible to the user
}

/// <summary>
/// A moderation case — logs every mod action with a case number,
/// who did it, who it was done to, the reason, and when.
/// </summary>
public class ModCase : DbEntity
{
    /// <summary>The guild this case belongs to.</summary>
    public ulong GuildId { get; set; }

    /// <summary>Auto-incrementing case number within the guild.</summary>
    public int CaseNumber { get; set; }

    /// <summary>What kind of action was taken.</summary>
    public ModCaseType CaseType { get; set; }

    /// <summary>The user who was actioned (target).</summary>
    public ulong TargetUserId { get; set; }

    /// <summary>The moderator who took the action.</summary>
    public ulong ModeratorUserId { get; set; }

    /// <summary>The reason for the action.</summary>
    public string Reason { get; set; }

    /// <summary>Duration in minutes for timed actions (TempMute, TempBan, TimeOut).</summary>
    public int DurationMinutes { get; set; }

    /// <summary>When the action was taken.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The message ID of the case log embed in the mod log channel.
    /// Used to update the embed when the reason is changed.
    /// </summary>
    public ulong? LogMessageId { get; set; }

    /// <summary>The channel ID where the log message was posted.</summary>
    public ulong? LogChannelId { get; set; }
}

/// <summary>
/// A private moderator note on a user. Only visible to mods, not the user.
/// </summary>
public class ModNote : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong TargetUserId { get; set; }
    public ulong ModeratorUserId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Configures automatic punishment when a user reaches X mod cases in Y time.
/// Similar to warnpunish but works on all case types, not just warnings.
/// </summary>
public class AutoPunishConfig : DbEntity
{
    public ulong GuildId { get; set; }

    /// <summary>Number of cases that triggers the auto-punishment.</summary>
    public int CaseCount { get; set; }

    /// <summary>Time window in hours. Only cases within this window count. 0 = all time.</summary>
    public int TimeWindowHours { get; set; }

    /// <summary>What action to take when threshold is reached.</summary>
    public PunishmentAction Action { get; set; }

    /// <summary>Duration in minutes for timed actions.</summary>
    public int ActionDurationMinutes { get; set; }
}

/// <summary>
/// Per-guild moderation settings.
/// </summary>
public class ModSettings : DbEntity
{
    public ulong GuildId { get; set; }

    /// <summary>Channel where mod case logs are posted.</summary>
    public ulong? ModLogChannelId { get; set; }

    /// <summary>Whether to DM users when they are kicked/banned/muted with the reason.</summary>
    public bool DmOnAction { get; set; } = true;

    /// <summary>Whether to auto-delete the mod command message after execution.</summary>
    public bool DeleteModCommands { get; set; }

    /// <summary>
    /// Comma-separated role IDs that are protected from mod actions.
    /// Users with these roles cannot be warned/muted/kicked/banned.
    /// </summary>
    public string ProtectedRoleIds { get; set; }

    /// <summary>
    /// Template for DM messages sent to users on mod action.
    /// Variables: {action}, {reason}, {server}, {duration}
    /// </summary>
    public string DmTemplate { get; set; } = "You have been **{action}** in **{server}**.\nReason: {reason}";
}

// ── Entity Framework Configurations ──

public class ModCaseEntityConfiguration : IEntityTypeConfiguration<ModCase>
{
    public void Configure(EntityTypeBuilder<ModCase> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.CaseNumber }).IsUnique();
        builder.HasIndex(x => new { x.GuildId, x.TargetUserId });
    }
}

public class ModNoteEntityConfiguration : IEntityTypeConfiguration<ModNote>
{
    public void Configure(EntityTypeBuilder<ModNote> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.TargetUserId });
    }
}

public class AutoPunishConfigEntityConfiguration : IEntityTypeConfiguration<AutoPunishConfig>
{
    public void Configure(EntityTypeBuilder<AutoPunishConfig> builder)
    {
        builder.HasIndex(x => x.GuildId);
        builder.ToTable("AutoPunishConfigs");
    }
}

public class ModSettingsEntityConfiguration : IEntityTypeConfiguration<ModSettings>
{
    public void Configure(EntityTypeBuilder<ModSettings> builder)
    {
        builder.HasIndex(x => x.GuildId).IsUnique();
    }
}
