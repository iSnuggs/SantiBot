#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

public enum AutobanRuleType
{
    AccountAge,     // Ban accounts younger than X
    Username,       // Ban users with specific words in name
    NoAvatar,       // Ban users with default avatar
}

/// <summary>
/// An autoban rule that automatically bans users when they join based on criteria.
/// </summary>
public class AutobanRule : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; } = true;
    public AutobanRuleType RuleType { get; set; }

    /// <summary>
    /// For AccountAge: minimum age in hours.
    /// For Username/NoAvatar: unused.
    /// </summary>
    public int MinAccountAgeHours { get; set; }

    /// <summary>
    /// For Username: words to match (newline-separated). Supports wildcards.
    /// </summary>
    public string UsernamePatterns { get; set; }

    /// <summary>The action to take: Ban, Kick, or TimeOut.</summary>
    public PunishmentAction Action { get; set; } = PunishmentAction.Ban;

    /// <summary>Custom reason shown in audit log.</summary>
    public string Reason { get; set; } = "Autoban rule triggered";
}

public class AutobanRuleEntityConfiguration : IEntityTypeConfiguration<AutobanRule>
{
    public void Configure(EntityTypeBuilder<AutobanRule> builder)
    {
        builder.HasIndex(x => x.GuildId);
    }
}
