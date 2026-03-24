#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// A TikTok user that a guild channel is following for new post notifications.
/// </summary>
public class TikTokFollow : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

    /// <summary>The TikTok username (without @).</summary>
    public string Username { get; set; }

    /// <summary>The ID of the last video we notified about.</summary>
    public string LastVideoId { get; set; }
}

public class TikTokFollowEntityConfiguration : IEntityTypeConfiguration<TikTokFollow>
{
    public void Configure(EntityTypeBuilder<TikTokFollow> builder)
    {
        builder.HasIndex(x => x.GuildId);
        builder.HasIndex(x => new { x.GuildId, x.ChannelId, x.Username }).IsUnique();
    }
}
