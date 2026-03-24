#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// The type of content filter an automod rule enforces.
/// </summary>
public enum AutomodFilterType
{
    BannedWords,        // Regex/wildcard word filter
    MassCaps,           // Messages with too many capital letters
    DuplicateText,      // Same message sent multiple times
    FastSpam,           // Too many messages in a short time
    MassMentions,       // Too many @mentions in one message
    EmojiSpam,          // Too many emojis in one message
    NewlineSpam,        // Too many newlines in one message
    AttachmentSpam,     // Too many attachments in a short time
    SpoilerAbuse,       // Excessive spoiler tags
    ZalgoText,          // Zalgo/glitch text
    ExternalEmoji,      // Emojis from other servers
    PhishingLinks,      // Known phishing/scam URLs
    MaskedLinks,        // Links hidden behind markdown text
    StickerSpam,        // Too many stickers in a short time
    InviteLinks,        // Discord invite links
    AllLinks,           // Any URL
    LinkBlacklist,      // Specific blocked domains
    LinkWhitelist,      // Only allow specific domains (block all others)
    SelfbotDetection,   // Detecting selfbot/automated behavior
}

/// <summary>
/// The action taken when an automod rule is triggered.
/// </summary>
public enum AutomodAction
{
    Delete,             // Delete the message
    Warn,               // Warn the user
    Mute,               // Mute the user
    TempMute,           // Temporarily mute the user
    Kick,               // Kick the user
    Ban,                // Ban the user
    TempBan,            // Temporarily ban the user
    TimeOut,            // Discord native timeout
    CustomResponse,     // Send a custom message
}

/// <summary>
/// A single automod rule for a guild. Each rule has a filter type,
/// action to take, and optional thresholds/configuration.
/// </summary>
public class AutomodRule : DbEntity
{
    public ulong GuildId { get; set; }

    /// <summary>Whether this rule is currently active.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The type of filter this rule enforces.</summary>
    public AutomodFilterType FilterType { get; set; }

    /// <summary>The action to take when this rule is triggered.</summary>
    public AutomodAction Action { get; set; } = AutomodAction.Delete;

    /// <summary>Duration in minutes for TempMute/TempBan/TimeOut actions.</summary>
    public int ActionDurationMinutes { get; set; }

    /// <summary>Custom response message (for CustomResponse action).</summary>
    public string CustomResponseText { get; set; }

    /// <summary>
    /// Threshold value — meaning depends on filter type:
    /// - MassCaps: percentage (e.g., 70 = 70% caps)
    /// - DuplicateText: number of identical messages
    /// - FastSpam: number of messages
    /// - MassMentions: number of mentions
    /// - EmojiSpam: number of emojis
    /// - NewlineSpam: number of newlines
    /// - AttachmentSpam: number of attachments
    /// - StickerSpam: number of stickers
    /// </summary>
    public int Threshold { get; set; } = 5;

    /// <summary>
    /// Time window in seconds for rate-based filters (FastSpam, DuplicateText, etc.)
    /// </summary>
    public int TimeWindowSeconds { get; set; } = 10;

    /// <summary>
    /// Pattern for BannedWords (regex/wildcard), or domain list for Link filters.
    /// Multiple entries separated by newlines.
    /// </summary>
    public string PatternOrList { get; set; }

    /// <summary>Exempted channels and roles for this rule.</summary>
    public List<AutomodRuleExemption> Exemptions { get; set; } = new();
}

/// <summary>
/// A channel or role that is exempt from a specific automod rule.
/// </summary>
public class AutomodRuleExemption : DbEntity
{
    public int AutomodRuleId { get; set; }

    /// <summary>The type of exemption (channel or role).</summary>
    public AutomodExemptionType Type { get; set; }

    /// <summary>The ID of the exempted channel or role.</summary>
    public ulong ExemptId { get; set; }
}

public enum AutomodExemptionType
{
    Channel,
    Role,
}

/// <summary>
/// Tracks escalating infractions for a user in a guild.
/// X infractions in Y time = escalated action.
/// </summary>
public class AutomodInfraction : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public AutomodFilterType FilterType { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
}

// ── Entity Framework Configuration ──

public class AutomodRuleEntityConfiguration : IEntityTypeConfiguration<AutomodRule>
{
    public void Configure(EntityTypeBuilder<AutomodRule> builder)
    {
        builder.HasIndex(x => x.GuildId);

        builder.HasMany(x => x.Exemptions)
               .WithOne()
               .HasForeignKey(x => x.AutomodRuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AutomodInfractionEntityConfiguration : IEntityTypeConfiguration<AutomodInfraction>
{
    public void Configure(EntityTypeBuilder<AutomodInfraction> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.UserId });
    }
}
