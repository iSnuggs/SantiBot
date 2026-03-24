#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// How the trigger text should be matched against messages.
/// </summary>
public enum AutoResponseTriggerType
{
    Contains,       // Message contains the trigger text anywhere
    ExactMatch,     // Message is exactly the trigger text
    StartsWith,     // Message starts with the trigger text
    Wildcard,       // Wildcard matching with * and ?
    Regex,          // Full regex matching
}

/// <summary>
/// What kind of response to send when triggered.
/// </summary>
public enum AutoResponseType
{
    Text,           // Send a plain text message
    Embed,          // Send an embed (JSON stored in ResponseText)
    DM,             // DM the user
    Reaction,       // Add a reaction to the triggering message
}

/// <summary>
/// An automatic response rule for a guild. When a message matches the trigger,
/// the bot responds with the configured response.
/// </summary>
public class AutoResponse : DbEntity
{
    public ulong GuildId { get; set; }

    /// <summary>Whether this auto-response is currently active.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The text/pattern that triggers this response.</summary>
    public string Trigger { get; set; }

    /// <summary>How to match the trigger against messages.</summary>
    public AutoResponseTriggerType TriggerType { get; set; } = AutoResponseTriggerType.Contains;

    /// <summary>The response text, embed JSON, or emoji to react with.</summary>
    public string ResponseText { get; set; }

    /// <summary>The type of response to send.</summary>
    public AutoResponseType ResponseType { get; set; } = AutoResponseType.Text;

    /// <summary>Whether to delete the message that triggered the response.</summary>
    public bool DeleteTrigger { get; set; }

    /// <summary>
    /// Per-user cooldown in seconds. 0 = no cooldown.
    /// Prevents the same user from triggering the response too often.
    /// </summary>
    public int UserCooldownSeconds { get; set; }

    /// <summary>
    /// Per-channel cooldown in seconds. 0 = no cooldown.
    /// Prevents the response from firing too often in the same channel.
    /// </summary>
    public int ChannelCooldownSeconds { get; set; }

    /// <summary>
    /// Comma-separated list of channel IDs where this response is allowed.
    /// Empty = all channels.
    /// </summary>
    public string AllowedChannelIds { get; set; }

    /// <summary>
    /// Comma-separated list of role IDs that can trigger this response.
    /// Empty = all roles.
    /// </summary>
    public string AllowedRoleIds { get; set; }
}

public class AutoResponseEntityConfiguration : IEntityTypeConfiguration<AutoResponse>
{
    public void Configure(EntityTypeBuilder<AutoResponse> builder)
    {
        builder.HasIndex(x => x.GuildId);
    }
}
