#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// Per-guild welcome image configuration. When enabled, generates a custom
/// welcome banner with the user's avatar, name, and server info.
/// </summary>
public class WelcomeImageConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }

    /// <summary>Channel where the welcome image is posted.</summary>
    public ulong? ChannelId { get; set; }

    /// <summary>URL to a custom background image. If null, uses a default gradient.</summary>
    public string BackgroundUrl { get; set; }

    /// <summary>Hex color for the accent/overlay (e.g., "0C95E9").</summary>
    public string AccentColor { get; set; } = "0C95E9";

    /// <summary>Custom welcome text. Variables: {user}, {server}, {membercount}.</summary>
    public string WelcomeText { get; set; } = "Welcome to {server}!";

    /// <summary>Custom subtitle text.</summary>
    public string SubtitleText { get; set; } = "You are member #{membercount}";
}

public class WelcomeImageConfigEntityConfiguration : IEntityTypeConfiguration<WelcomeImageConfig>
{
    public void Configure(EntityTypeBuilder<WelcomeImageConfig> builder)
    {
        builder.HasIndex(x => x.GuildId).IsUnique();
    }
}
