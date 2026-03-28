#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// A Bluesky account that a guild channel is following for new post notifications.
/// </summary>
public class BlueskyFeedSub : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

    /// <summary>The Bluesky handle (e.g. user.bsky.social).</summary>
    public string BlueskyHandle { get; set; } = "";

    /// <summary>The AT URI of the last post we notified about.</summary>
    public string LastPostUri { get; set; } = "";

    public DateTime LastChecked { get; set; }
}

public class BlueskyFeedSubEntityConfiguration : IEntityTypeConfiguration<BlueskyFeedSub>
{
    public void Configure(EntityTypeBuilder<BlueskyFeedSub> builder)
    {
        builder.HasIndex(x => x.GuildId);
        builder.HasIndex(x => new { x.GuildId, x.ChannelId, x.BlueskyHandle }).IsUnique();
    }
}
