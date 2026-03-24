#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// Links a voice channel to a text channel. When a user joins the voice channel,
/// they get access to the text channel. When they leave, access is revoked.
/// </summary>
public class VoiceTextLink : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong VoiceChannelId { get; set; }
    public ulong TextChannelId { get; set; }
}

/// <summary>
/// Per-guild auto-dehoist configuration.
/// Automatically removes special characters from nicknames that push users
/// to the top of the member list.
/// </summary>
public class DehoistConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }

    /// <summary>What to replace the hoisting characters with. Empty = just remove them.</summary>
    public string ReplacementPrefix { get; set; } = "";
}

public class VoiceTextLinkEntityConfiguration : IEntityTypeConfiguration<VoiceTextLink>
{
    public void Configure(EntityTypeBuilder<VoiceTextLink> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.VoiceChannelId }).IsUnique();
    }
}

public class DehoistConfigEntityConfiguration : IEntityTypeConfiguration<DehoistConfig>
{
    public void Configure(EntityTypeBuilder<DehoistConfig> builder)
    {
        builder.HasIndex(x => x.GuildId).IsUnique();
    }
}
