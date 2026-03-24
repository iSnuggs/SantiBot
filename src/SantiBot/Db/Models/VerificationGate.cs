#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

/// <summary>
/// Per-guild verification gate configuration.
/// New members must verify (react/button) before getting access.
/// </summary>
public class VerificationGate : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }

    /// <summary>The channel where the verification message is posted.</summary>
    public ulong? VerifyChannelId { get; set; }

    /// <summary>The message ID of the verification panel.</summary>
    public ulong? VerifyMessageId { get; set; }

    /// <summary>The role to grant upon verification.</summary>
    public ulong? VerifiedRoleId { get; set; }

    /// <summary>Custom message on the verification panel.</summary>
    public string VerifyMessage { get; set; } = "Click the button below to verify and get access to the server!";

    /// <summary>
    /// Whether to enable auto-lockdown mode.
    /// When X users join within Y seconds, automatically restrict the server.
    /// </summary>
    public bool AutoLockdownEnabled { get; set; }

    /// <summary>Number of joins that triggers lockdown.</summary>
    public int LockdownJoinThreshold { get; set; } = 10;

    /// <summary>Time window in seconds for the join threshold.</summary>
    public int LockdownTimeWindowSeconds { get; set; } = 10;

    /// <summary>Whether the server is currently in lockdown.</summary>
    public bool IsLockedDown { get; set; }
}

public class VerificationGateEntityConfiguration : IEntityTypeConfiguration<VerificationGate>
{
    public void Configure(EntityTypeBuilder<VerificationGate> builder)
    {
        builder.HasIndex(x => x.GuildId).IsUnique();
    }
}
