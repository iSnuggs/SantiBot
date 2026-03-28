#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// A Kick.com streamer that a guild channel is following for live notifications.
/// </summary>
public class KickStreamFollow : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong NotifyChannelId { get; set; }

    /// <summary>The Kick.com username.</summary>
    public string KickUsername { get; set; } = "";

    /// <summary>Whether the streamer is currently live (to avoid duplicate notifications).</summary>
    public bool IsLive { get; set; }

    /// <summary>Optional custom message to include when the streamer goes live.</summary>
    public string CustomMessage { get; set; } = "";
}

public class KickStreamFollowEntityConfiguration : IEntityTypeConfiguration<KickStreamFollow>
{
    public void Configure(EntityTypeBuilder<KickStreamFollow> builder)
    {
        builder.HasIndex(x => x.GuildId);
        builder.HasIndex(x => new { x.GuildId, x.NotifyChannelId, x.KickUsername }).IsUnique();
    }
}
