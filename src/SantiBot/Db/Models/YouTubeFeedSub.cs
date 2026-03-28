#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// A YouTube channel that a guild channel is following for new upload notifications.
/// </summary>
public class YouTubeFeedSub : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

    /// <summary>The YouTube channel ID (UCxxxx format).</summary>
    public string YouTubeChannelId { get; set; } = "";

    /// <summary>The YouTube channel display name.</summary>
    public string YouTubeChannelName { get; set; } = "";

    /// <summary>The ID of the last video we notified about.</summary>
    public string LastVideoId { get; set; } = "";

    public DateTime LastChecked { get; set; }
}

public class YouTubeFeedSubEntityConfiguration : IEntityTypeConfiguration<YouTubeFeedSub>
{
    public void Configure(EntityTypeBuilder<YouTubeFeedSub> builder)
    {
        builder.HasIndex(x => x.GuildId);
        builder.HasIndex(x => new { x.GuildId, x.ChannelId, x.YouTubeChannelId }).IsUnique();
    }
}
