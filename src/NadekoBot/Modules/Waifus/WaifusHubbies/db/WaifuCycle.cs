using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NadekoBot.Modules.Waifus.WaifusHubbies.Db;

/// <summary>
/// Records a payout cycle for a waifu.
/// </summary>
public class WaifuCycle
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Discord User ID of the waifu.
    /// </summary>
    public ulong WaifuUserId { get; set; }

    /// <summary>
    /// Cycle number (cycles since epoch).
    /// </summary>
    public int CycleNumber { get; set; }

    /// <summary>
    /// Manager for this cycle.
    /// </summary>
    public ulong ManagerUserId { get; set; }

    /// <summary>
    /// Total backed amount snapshot.
    /// </summary>
    public long TotalBacked { get; set; }

    /// <summary>
    /// Total returns this cycle.
    /// </summary>
    public long TotalReturns { get; set; }

    /// <summary>
    /// Waifu's earnings (after manager cut).
    /// </summary>
    public long WaifuEarnings { get; set; }

    /// <summary>
    /// Manager's earnings.
    /// </summary>
    public long ManagerEarnings { get; set; }

    /// <summary>
    /// Total distributed to fans.
    /// </summary>
    public long FanPool { get; set; }

    /// <summary>
    /// Mood at cycle start.
    /// </summary>
    public int MoodSnapshot { get; set; }

    /// <summary>
    /// Food at cycle start.
    /// </summary>
    public int FoodSnapshot { get; set; }

    /// <summary>
    /// When this cycle was processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity configuration for WaifuCycle.
/// </summary>
public class WaifuCycleEntityConfiguration : IEntityTypeConfiguration<WaifuCycle>
{
    public void Configure(EntityTypeBuilder<WaifuCycle> builder)
    {
        builder.HasIndex(x => new { x.WaifuUserId, x.CycleNumber }).IsUnique();
    }
}
