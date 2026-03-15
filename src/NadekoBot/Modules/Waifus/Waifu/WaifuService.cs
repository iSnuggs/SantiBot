using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Waifus.Waifu.Db;
using NadekoBot.Modules.Games.Quests;
using OneOf;
using OneOf.Types;

namespace NadekoBot.Modules.Waifus.Waifu;

#region Result Types

public readonly struct ErrSelfNotAllowed;
public readonly struct ErrNoActionsLeft;
public readonly struct ErrWaifuNotFound;
public readonly struct ErrAlreadyOptedIn;
public readonly struct ErrNotOptedIn;
public readonly struct ErrInsufficientFunds;
public readonly struct ErrAlreadyBacking;
public readonly struct ErrNotBacking;
public readonly struct ErrHasFans;
public readonly struct ErrNotManager;
public readonly struct ErrInvalidPercent;
public readonly struct ErrOutsideBuyWindow;
public readonly struct ErrPriceTooLow;

[GenerateOneOf]
public sealed partial class OptInResult : OneOfBase<ErrAlreadyOptedIn, ErrInsufficientFunds, Success>;

[GenerateOneOf]
public sealed partial class OptOutResult : OneOfBase<ErrNotOptedIn, ErrHasFans, Success>;

[GenerateOneOf]
public sealed partial class BackResult : OneOfBase<ErrAlreadyBacking, ErrWaifuNotFound, Success>;

[GenerateOneOf]
public sealed partial class StopBackingResult : OneOfBase<ErrNotBacking, Success>;

[GenerateOneOf]
public sealed partial class ImproveMoodResult : OneOfBase<ErrSelfNotAllowed, ErrNoActionsLeft, ErrWaifuNotFound, Success<int>>;

[GenerateOneOf]
public sealed partial class SetFeeResult : OneOfBase<ErrWaifuNotFound, ErrNotOptedIn, ErrInvalidPercent, Success>;

public readonly struct ErrItemNotAvailable;

[GenerateOneOf]
public sealed partial class GiftResult : OneOfBase<ErrSelfNotAllowed, ErrInsufficientFunds, ErrWaifuNotFound, ErrItemNotAvailable, Success<WaifuGiftItem>>;

[GenerateOneOf]
public sealed partial class BuyManagerResult : OneOfBase<ErrOutsideBuyWindow, ErrInsufficientFunds, ErrWaifuNotFound, ErrPriceTooLow, Success<BuyManagerInfo>>;

[GenerateOneOf]
public sealed partial class ResignManagerResult : OneOfBase<ErrNotManager, Success>;

public readonly struct ErrNoPendingPayout;

[GenerateOneOf]
public sealed partial class ClaimPayoutResult : OneOfBase<ErrNoPendingPayout, Success<long>>;

/// <summary>
/// Details about a successful manager purchase.
/// </summary>
public sealed class BuyManagerInfo
{
    public long PricePaid { get; init; }
    public ulong? OldManagerId { get; init; }
    public long OldManagerPayout { get; init; }
    public long WaifuPayout { get; init; }
    public long Burned { get; init; }
}

/// <summary>
/// Details about a manager exit for confirmation display.
/// </summary>
public sealed class ManagerExitInfo
{
    public long Refund { get; init; }
    public long WaifuCut { get; init; }
    public long FanDistribution { get; init; }
    public long Burned { get; init; }
    public long NewPrice { get; init; }
}

#endregion

/// <summary>
/// Service for managing the Waifu backing system.
/// </summary>
public sealed class WaifuService(
    DbService db,
    IBotCache cache,
    ICurrencyService cs,
    DiscordSocketClient client,
    WaifuConfigService configService,
    QuestService? quests = null,
    TimeProvider? timeProvider = null
) : INService, IReadyExecutor
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    private double CycleHours => configService.Data.CycleHours;
    private double BaseReturnRate => configService.Data.BaseReturnRate;
    private double CyclesPerYear => 365.25 * 24 / CycleHours;

    /// <summary>
    /// Gets the managerless price decay percentage (0-100) from config.
    /// </summary>
    public int ManagerlessDecayPercent => configService.Data.ManagerlessDecayPercent;

    /// <summary>
    /// Gets the maximum number of mood/food actions per user per day.
    /// </summary>
    public int MaxActions => configService.Data.MaxDailyActions;

    /// <summary>
    /// Gets all gift items from config as WaifuGiftItem records.
    /// </summary>
    public IReadOnlyList<WaifuGiftItem> GetAllItems()
        => WaifuGiftItems.FromConfig(configService.Data.Items);

    /// <summary>
    /// Epoch: Tuesday, January 7, 2025 00:00 UTC.
    /// </summary>
    private static readonly DateTime Epoch = new(2025, 1, 7, 0, 0, 0, DateTimeKind.Utc);

    #region Initialization

    /// <summary>
    /// Initializes background loops on bot ready.
    /// </summary>
    public Task OnReadyAsync()
    {
        if (client.ShardId != 0)
            return Task.CompletedTask;

        _ = Task.Run(DecayLoopAsync);
        _ = Task.Run(CycleLoopAsync);

        return Task.CompletedTask;
    }

    #endregion

    #region Cycle Calculations

    private static TypedKey<int> GetActionsKey(ulong userId)
        => new($"wnh:actions:{userId}");

    /// <summary>
    /// Gets the current cycle number.
    /// </summary>
    public int GetCurrentCycle()
        => (int)((_timeProvider.GetUtcNow().UtcDateTime - Epoch).TotalHours / CycleHours);

    /// <summary>
    /// Gets when a cycle starts.
    /// </summary>
    public DateTime GetCycleStartTime(int? cycle = null)
    {
        var c = cycle ?? GetCurrentCycle();
        return Epoch.AddHours(c * CycleHours);
    }

    /// <summary>
    /// Gets when the next cycle starts.
    /// </summary>
    public DateTime GetNextCycleTime()
        => GetCycleStartTime(GetCurrentCycle() + 1);

    /// <summary>
    /// Checks if we're within the buy window (first N hours of cycle).
    /// </summary>
    public bool IsWithinBuyWindow()
        => _timeProvider.GetUtcNow().UtcDateTime < GetCycleStartTime().AddHours(configService.Data.BuyWindowHours);

    /// <summary>
    /// Gets the fraction of the current cycle that has elapsed (0.0 to 1.0).
    /// </summary>
    public double GetCycleProgressFraction()
    {
        var start = GetCycleStartTime();
        var elapsed = (_timeProvider.GetUtcNow().UtcDateTime - start).TotalHours;
        return Math.Clamp(elapsed / CycleHours, 0.0, 1.0);
    }

    /// <summary>
    /// Gets the time remaining until the next cycle payout.
    /// </summary>
    public TimeSpan GetTimeUntilPayout()
    {
        var next = GetNextCycleTime();
        var remaining = next - _timeProvider.GetUtcNow().UtcDateTime;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets waifu info for display, using cycle snapshots for backed amounts.
    /// </summary>
    public async Task<WaifuInfoDto?> GetWaifuInfoAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return null;

        var currentCycle = GetCurrentCycle();

        var activeFanIds = await ctx.GetTable<WaifuFan>()
            .Where(x => x.WaifuUserId == userId)
            .Select(x => x.UserId)
            .ToListAsyncLinqToDB();

        var snapshotFanIds = await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.WaifuUserId == userId && x.CycleNumber == currentCycle)
            .Select(x => x.UserId)
            .ToListAsyncLinqToDB();

        var snapshotTotalBacked = await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.WaifuUserId == userId && x.CycleNumber == currentCycle)
            .SumAsyncLinqToDB(x => x.SnapshotBalance);

        var snapshotSet = snapshotFanIds.ToHashSet();
        var activeSet = activeFanIds.ToHashSet();

        var snapshotExists = snapshotFanIds.Count > 0;
        var pendingJoins = snapshotExists ? activeSet.Count(id => !snapshotSet.Contains(id)) : 0;
        var pendingLeaves = snapshotExists ? snapshotSet.Count(id => !activeSet.Contains(id)) : 0;

        var lastCycleReturns = await ctx.GetTable<WaifuCycle>()
            .Where(x => x.WaifuUserId == userId && x.CycleNumber == currentCycle - 1)
            .Select(x => x.TotalReturns)
            .FirstOrDefaultAsyncLinqToDB();

        return new WaifuInfoDto
        {
            UserId = wi.UserId,
            Mood = wi.Mood,
            Food = wi.Food,
            WaifuFeePercent = wi.WaifuFeePercent,
            Price = wi.Price,
            TotalProduced = wi.TotalProduced,
            ReturnsCap = wi.ReturnsCap,
            FanCount = activeFanIds.Count,
            SnapshotTotalBacked = snapshotTotalBacked,
            LastCycleReturns = lastCycleReturns,
            PendingJoins = pendingJoins,
            PendingLeaves = pendingLeaves,
            ManagerId = wi.ManagerUserId,
            IsHubby = wi.IsHubby,
            CustomAvatarUrl = wi.CustomAvatarUrl,
            Description = wi.Description,
            Quote = wi.Quote
        };
    }

    /// <summary>
    /// Gets which waifu a user is backing.
    /// </summary>
    public async Task<ulong?> GetBackingAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();
        return await ctx.GetTable<WaifuFan>()
            .Where(x => x.UserId == userId)
            .Select(x => (ulong?)x.WaifuUserId)
            .FirstOrDefaultAsyncLinqToDB();
    }

    /// <summary>
    /// Checks whether the user is a waifu (opted in).
    /// </summary>
    public async Task<bool> IsWaifuAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();
        return await ctx.GetTable<WaifuInfo>()
            .AnyAsyncLinqToDB(x => x.UserId == userId);
    }

    /// <summary>
    /// Checks whether the user is a fan (backing someone).
    /// </summary>
    public async Task<bool> IsFanAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();
        return await ctx.GetTable<WaifuFan>()
            .AnyAsyncLinqToDB(x => x.UserId == userId);
    }

    /// <summary>
    /// Checks whether the user is the manager of any waifu.
    /// </summary>
    public async Task<bool> IsManagerAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();
        return await ctx.GetTable<WaifuInfo>()
            .AnyAsyncLinqToDB(x => x.ManagerUserId == userId);
    }

    /// <summary>
    /// Gets the manager exit info for confirmation display for a specific waifu.
    /// </summary>
    public async Task<ManagerExitInfo?> GetManagerExitInfoAsync(ulong userId, ulong waifuUserId)
    {
        await using var ctx = db.GetDbContext();
        var conf = configService.Data;

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuUserId && x.ManagerUserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return null;

        var price = wi.Price;
        return new ManagerExitInfo
        {
            Refund = (long)(price * conf.ManagerExitRefund),
            WaifuCut = (long)(price * conf.ManagerExitWaifu),
            FanDistribution = (long)(price * conf.ManagerExitFans),
            Burned = (long)(price * (1.0 - conf.ManagerExitRefund - conf.ManagerExitWaifu - conf.ManagerExitFans)),
            NewPrice = (long)(price * conf.ManagerExitRefund)
        };
    }

    /// <summary>
    /// Gets all fans of a waifu with their last cycle earnings and pending status.
    /// </summary>
    public async Task<List<FanInfoDto>> GetFansAsync(ulong waifuUserId)
    {
        await using var ctx = db.GetDbContext();
        var currentCycle = GetCurrentCycle();
        var lastCycle = currentCycle - 1;

        var fans = await ctx.GetTable<WaifuFan>()
            .Where(x => x.WaifuUserId == waifuUserId)
            .ToListAsyncLinqToDB();

        var snapshotFanIds = await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.WaifuUserId == waifuUserId && x.CycleNumber == currentCycle)
            .Select(x => x.UserId)
            .ToListAsyncLinqToDB();

        var snapshotSet = snapshotFanIds.ToHashSet();

        var lastCycleData = await ctx.GetTable<WaifuCycle>()
            .Where(x => x.WaifuUserId == waifuUserId && x.CycleNumber == lastCycle)
            .FirstOrDefaultAsyncLinqToDB();

        var lastCycleSnapshots = lastCycleData is not null
            ? await ctx.GetTable<WaifuCycleSnapshot>()
                .Where(x => x.WaifuUserId == waifuUserId && x.CycleNumber == lastCycle)
                .ToListAsyncLinqToDB()
            : [];

        var lastCycleTotalBacked = lastCycleSnapshots.Sum(x => x.SnapshotBalance);

        var result = new List<FanInfoDto>();
        foreach (var fan in fans)
        {
            long lastEarnings = 0;

            if (lastCycleData is not null && lastCycleTotalBacked > 0)
            {
                var fanSnapshot = lastCycleSnapshots.FirstOrDefault(x => x.UserId == fan.UserId);
                if (fanSnapshot is not null)
                    lastEarnings = (long)(lastCycleData.FanPool * ((double)fanSnapshot.SnapshotBalance / lastCycleTotalBacked));
            }

            result.Add(new FanInfoDto
            {
                UserId = fan.UserId,
                LastCycleEarnings = lastEarnings,
                IsPending = !snapshotSet.Contains(fan.UserId),
                BackedAt = fan.DelegatedAt
            });
        }

        return result.OrderByDescending(x => x.LastCycleEarnings).ToList();
    }

    /// <summary>
    /// Gets remaining actions for a user today.
    /// </summary>
    public async Task<int> GetRemainingActionsAsync(ulong userId)
    {
        var actionsKey = GetActionsKey(userId);
        var actionsUsed = await cache.GetAsync(actionsKey);
        return configService.Data.MaxDailyActions - (actionsUsed.TryPickT0(out var used, out _) ? used : 0);
    }

    /// <summary>
    /// Gets leaderboard of all waifus ordered by the specified criteria.
    /// </summary>
    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(WaifuLbOrder order = WaifuLbOrder.Backing)
    {
        await using var ctx = db.GetDbContext();
        var currentCycle = GetCurrentCycle();
        var startCycle = currentCycle - 8;

        var cycleAgg = ctx.GetTable<WaifuCycle>()
            .Where(x => x.CycleNumber > startCycle)
            .GroupBy(x => x.WaifuUserId)
            .Select(g => new
            {
                WaifuUserId = g.Key,
                TotalReturns = g.Sum(x => x.TotalReturns),
                AvgBacked = g.Average(x => (double)x.TotalBacked),
                CyclesActive = g.Count()
            });

        var snapAgg = ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.CycleNumber == currentCycle)
            .GroupBy(x => x.WaifuUserId)
            .Select(g => new
            {
                WaifuUserId = g.Key,
                TotalBacked = g.Sum(x => x.SnapshotBalance)
            });

        var query = ctx.GetTable<WaifuInfo>()
            .LeftJoin(cycleAgg, (wi, ca) => wi.UserId == ca.WaifuUserId, (wi, ca) => new { wi, ca })
            .LeftJoin(snapAgg, (x, sa) => x.wi.UserId == sa.WaifuUserId, (x, sa) => new { x.wi, x.ca, sa })
            .LeftJoin(ctx.GetTable<DiscordUser>(), (x, du) => x.wi.UserId == du.UserId, (x, du) => new { x.wi, x.ca, x.sa, du });

        var rows = await query.ToListAsyncLinqToDB();

        return rows.Select(r =>
        {
            var totalReturns = r.ca?.TotalReturns ?? 0;
            var avgBacked = r.ca?.AvgBacked ?? 0;
            return new LeaderboardEntryDto
            {
                UserId = r.wi.UserId,
                Username = r.du?.Username,
                Price = r.wi.Price,
                Mood = r.wi.Mood,
                Food = r.wi.Food,
                ReturnsCap = r.wi.ReturnsCap,
                RealizedReturnRate = avgBacked > 0 ? (totalReturns / avgBacked) * CyclesPerYear : 0,
                TotalReturns = totalReturns,
                AvgBacked = (long)avgBacked,
                CyclesActive = r.ca?.CyclesActive ?? 0,
                SnapshotTotalBacked = r.sa?.TotalBacked ?? 0,
                HasManager = r.wi.ManagerUserId is not null
            };
        }).OrderByDescending(x => order == WaifuLbOrder.Price ? x.Price : x.SnapshotTotalBacked).ToList();
    }

    /// <summary>
    /// Projects the payout for the current cycle using snapshot balances and current stats.
    /// Falls back to live data if no snapshot exists yet.
    /// </summary>
    public async Task<PayoutProjectionDto?> GetProjectedPayoutAsync(ulong waifuUserId)
    {
        await using var ctx = db.GetDbContext();

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuUserId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return null;

        if (wi.ManagerUserId is null)
            return new PayoutProjectionDto();

        var currentCycle = GetCurrentCycle();

        var snapshotRows = await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.WaifuUserId == waifuUserId && x.CycleNumber == currentCycle)
            .ToListAsyncLinqToDB();

        long totalBacked;

        if (snapshotRows.Count > 0)
        {
            totalBacked = snapshotRows.Sum(x => x.SnapshotBalance);
        }
        else
        {
            // Fallback: no snapshot yet (first cycle or bot just started)
            var fanIds = await ctx.GetTable<WaifuFan>()
                .Where(x => x.WaifuUserId == waifuUserId)
                .Select(x => x.UserId)
                .ToListAsyncLinqToDB();

            totalBacked = 0;
            foreach (var fanId in fanIds)
            {
                var balance = await ctx.GetTable<BankUser>()
                    .Where(x => x.UserId == fanId)
                    .Select(x => x.Balance)
                    .FirstOrDefaultAsyncLinqToDB();

                totalBacked += balance;
            }
        }

        if (totalBacked <= 0)
            return new PayoutProjectionDto();

        var effectiveRate = BaseReturnRate;
        var cycleRate = effectiveRate / CyclesPerYear;
        var statMultiplier = (wi.Mood + wi.Food) / 2000.0;
        var cappedBacked = Math.Min(totalBacked, wi.ReturnsCap);
        var returns = (long)(cappedBacked * cycleRate * statMultiplier);

        if (returns <= 0)
            return new PayoutProjectionDto { EffectiveRate = effectiveRate };

        var waifuCut = (long)(returns * wi.WaifuFeePercent / 100.0);
        var managerCut = (long)(waifuCut * configService.Data.ManagerCutPercent);
        var waifuNet = waifuCut - managerCut;
        var fanPool = returns - waifuCut;

        return new PayoutProjectionDto
        {
            TotalReturns = returns,
            WaifuCut = waifuNet,
            ManagerCut = managerCut,
            FanPool = fanPool,
            EffectiveRate = effectiveRate
        };
    }

    #endregion

    #region Opt In/Out

    /// <summary>
    /// Opts a user into the waifu system.
    /// </summary>
    public async Task<OptInResult> OptInAsync(ulong userId, bool isHubby = false)
    {
        await using var ctx = db.GetDbContext();
        var conf = configService.Data;

        var existing = await ctx.GetTable<WaifuInfo>()
            .AnyAsyncLinqToDB(x => x.UserId == userId);

        if (existing)
            return new ErrAlreadyOptedIn();

        if (!await cs.RemoveAsync(userId, conf.OptInCost, new("waifu", "opt-in")))
            return new ErrInsufficientFunds();

        await ctx.GetTable<WaifuInfo>()
            .InsertAsync(() => new()
            {
                UserId = userId,
                IsHubby = isHubby,
                Mood = 500,
                Food = 500,
                WaifuFeePercent = 5,
                Price = conf.OptInCost / 10,
                ReturnsCap = conf.DefaultReturnsCap,
                LastDecayTime = _timeProvider.GetUtcNow().UtcDateTime,
                TotalProduced = 0
            });

        return new Success();
    }

    /// <summary>
    /// Opts a user out of the waifu system.
    /// </summary>
    public async Task<OptOutResult> OptOutAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();

        var hasFans = await ctx.GetTable<WaifuFan>()
            .AnyAsyncLinqToDB(x => x.WaifuUserId == userId);

        if (hasFans)
            return new ErrHasFans();

        var deleted = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == userId)
            .DeleteAsync();

        if (deleted == 0)
            return new ErrNotOptedIn();

        return new Success();
    }

    #endregion

    #region Backing

    /// <summary>
    /// Becomes a fan of a waifu, or switches to a new waifu.
    /// </summary>
    public async Task<BackResult> BecomeFanAsync(ulong userId, ulong waifuUserId)
    {
        await using var ctx = db.GetDbContext();

        var existingFan = await ctx.GetTable<WaifuFan>()
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existingFan is not null)
        {
            if (existingFan.WaifuUserId == waifuUserId)
                return new ErrAlreadyBacking();

            await ctx.GetTable<WaifuFan>()
                .Where(x => x.UserId == userId)
                .DeleteAsync();
        }

        var waifuExists = await ctx.GetTable<WaifuInfo>()
            .AnyAsyncLinqToDB(x => x.UserId == waifuUserId);

        if (!waifuExists)
            return new ErrWaifuNotFound();

        await ctx.GetTable<WaifuFan>()
            .InsertAsync(() => new()
            {
                UserId = userId,
                WaifuUserId = waifuUserId,
                DelegatedAt = _timeProvider.GetUtcNow().UtcDateTime
            });

        return new Success();
    }

    /// <summary>
    /// Stops being a fan.
    /// </summary>
    public async Task<StopBackingResult> StopBeingFanAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();

        var deleted = await ctx.GetTable<WaifuFan>()
            .Where(x => x.UserId == userId)
            .DeleteAsync();

        if (deleted == 0)
            return new ErrNotBacking();

        return new Success();
    }

    /// <summary>
    /// Processes the financial consequences of a manager voluntarily exiting.
    /// </summary>
    private async Task ProcessManagerExitAsync(NadekoContext ctx, WaifuInfo waifu, ulong managerUserId)
    {
        var conf = configService.Data;
        var price = waifu.Price;
        var refund = (long)(price * conf.ManagerExitRefund);
        var waifuCut = (long)(price * conf.ManagerExitWaifu);
        var fanDistribution = (long)(price * conf.ManagerExitFans);

        // Refund manager
        if (refund > 0)
            await cs.AddAsync(managerUserId, refund, new("waifu", "manager-exit-refund"));

        // Pay waifu
        if (waifuCut > 0)
            await cs.AddAsync(waifu.UserId, waifuCut, new("waifu", "manager-exit-waifu"));

        // Distribute to other fans
        if (fanDistribution > 0)
        {
            var fanBalances = await ctx.GetTable<WaifuFan>()
                .Where(x => x.WaifuUserId == waifu.UserId && x.UserId != managerUserId)
                .LeftJoin(ctx.GetTable<BankUser>(),
                    (f, b) => f.UserId == b.UserId,
                    (f, b) => new { f.UserId, Balance = b != null ? b.Balance : 0L })
                .ToListAsyncLinqToDB();

            var totalOtherBacked = fanBalances.Sum(x => x.Balance);

            if (totalOtherBacked > 0)
            {
                foreach (var fan in fanBalances)
                {
                    var share = (long)(fanDistribution * ((double)fan.Balance / totalOtherBacked));
                    if (share > 0)
                        await cs.AddAsync(fan.UserId, share, new("waifu", "manager-exit-dist"));
                }
            }
        }

        // Drop price and remove manager
        var newPrice = Math.Max(conf.MinPrice, refund);
        await ctx.GetTable<WaifuInfo>()
            .Where(x => x.Id == waifu.Id)
            .Set(x => x.ManagerUserId, (ulong?)null)
            .Set(x => x.Price, newPrice)
            .UpdateAsync();
    }

    #endregion

    #region Manager Resign

    /// <summary>
    /// Resigns from managing a specific waifu. Triggers the manager exit financial flow.
    /// </summary>
    public async Task<ResignManagerResult> ResignManagerAsync(ulong userId, ulong waifuUserId)
    {
        await using var ctx = db.GetDbContext();

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuUserId && x.ManagerUserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return new ErrNotManager();

        await ProcessManagerExitAsync(ctx, wi, userId);

        return new Success();
    }

    /// <summary>
    /// Gets all waifus managed by the specified user, ordered by price descending.
    /// </summary>
    public async Task<List<ManagedWaifuDto>> GetManagedWaifusAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();
        var currentCycle = GetCurrentCycle();

        var snapAgg = ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.CycleNumber == currentCycle)
            .GroupBy(x => x.WaifuUserId)
            .Select(g => new
            {
                WaifuUserId = g.Key,
                TotalBacked = g.Sum(x => x.SnapshotBalance)
            });

        var query = ctx.GetTable<WaifuInfo>()
            .Where(x => x.ManagerUserId == userId)
            .LeftJoin(snapAgg, (wi, sa) => wi.UserId == sa.WaifuUserId, (wi, sa) => new { wi, sa })
            .LeftJoin(ctx.GetTable<DiscordUser>(), (x, du) => x.wi.UserId == du.UserId, (x, du) => new { x.wi, x.sa, du });

        var rows = await query.ToListAsyncLinqToDB();

        return rows.Select(r => new ManagedWaifuDto
        {
            WaifuUserId = r.wi.UserId,
            Username = r.du?.Username,
            Price = r.wi.Price,
            TotalBacked = r.sa?.TotalBacked ?? 0
        }).OrderByDescending(x => x.Price).ToList();
    }

    #endregion

    #region Manager Purchase

    /// <summary>
    /// Buys the manager position for a waifu.
    /// </summary>
    public async Task<BuyManagerResult> BuyManagerAsync(ulong buyerUserId, ulong waifuUserId)
    {
        await using var ctx = db.GetDbContext();
        var conf = configService.Data;

        if (!IsWithinBuyWindow())
            return new ErrOutsideBuyWindow();

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuUserId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return new ErrWaifuNotFound();

        var requiredPrice = (long)Math.Ceiling(wi.Price * (1 + conf.ManagerBuyPremium));

        if (!await cs.RemoveAsync(buyerUserId, requiredPrice, new("waifu", "manager-buy")))
            return new ErrInsufficientFunds();

        var oldManagerId = wi.ManagerUserId;
        var oldManagerPayout = (long)(requiredPrice * conf.ManagerOldPayout);
        var waifuPayout = (long)(requiredPrice * conf.ManagerWaifuPayout);
        var burned = requiredPrice - oldManagerPayout - waifuPayout;

        // Atomic update: only succeed if manager hasn't changed since we read
        int updated;
        if (oldManagerId.HasValue)
        {
            updated = await ctx.GetTable<WaifuInfo>()
                .Where(x => x.Id == wi.Id && x.ManagerUserId == oldManagerId.Value)
                .Set(x => x.ManagerUserId, buyerUserId)
                .Set(x => x.Price, requiredPrice)
                .UpdateAsync();
        }
        else
        {
            updated = await ctx.GetTable<WaifuInfo>()
                .Where(x => x.Id == wi.Id && x.ManagerUserId == null)
                .Set(x => x.ManagerUserId, buyerUserId)
                .Set(x => x.Price, requiredPrice)
                .UpdateAsync();
        }

        if (updated == 0)
        {
            // Another buyer won the race - refund
            await cs.AddAsync(buyerUserId, requiredPrice, new("waifu", "manager-buy-refund"));
            return new ErrPriceTooLow();
        }

        // Pay old manager (if exists)
        if (oldManagerId.HasValue && oldManagerPayout > 0)
            await cs.AddAsync(oldManagerId.Value, oldManagerPayout, new("waifu", "manager-buyout"));
        else
        {
            // First manager: waifu gets the old manager's share
            waifuPayout += oldManagerPayout;
        }

        // Pay waifu
        if (waifuPayout > 0)
            await cs.AddAsync(waifuUserId, waifuPayout, new("waifu", "manager-buy-fee"));

        return new Success<BuyManagerInfo>(new BuyManagerInfo
        {
            PricePaid = requiredPrice,
            OldManagerId = oldManagerId,
            OldManagerPayout = oldManagerId.HasValue ? oldManagerPayout : 0,
            WaifuPayout = waifuPayout,
            Burned = burned
        });
    }

    #endregion

    #region Waifu Fee

    /// <summary>
    /// Sets the waifu fee (waifu sets their own fee).
    /// </summary>
    public async Task<SetFeeResult> SetWaifuFeeAsync(ulong waifuUserId, int percent)
    {
        if (percent < 1 || percent > 5)
            return new ErrInvalidPercent();

        await using var ctx = db.GetDbContext();

        var updated = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuUserId)
            .Set(x => x.WaifuFeePercent, percent)
            .UpdateAsync();

        if (updated == 0)
            return new ErrNotOptedIn();

        return new Success();
    }

    #endregion

    #region Mood Actions

    /// <summary>
    /// Improves the mood of a waifu through an action.
    /// </summary>
    public async Task<ImproveMoodResult> ImproveMoodAsync(ulong userId, ulong waifuId, WaifuAction action)
    {
        if (userId == waifuId)
            return new ErrSelfNotAllowed();

        var conf = configService.Data;
        var actionsKey = GetActionsKey(userId);
        var actionsUsed = await cache.GetAsync(actionsKey);
        var used = actionsUsed.TryPickT0(out var u, out _) ? u : 0;

        if (used >= conf.MaxDailyActions)
            return new ErrNoActionsLeft();

        var baseMood = conf.BaseMoodIncrease;
        var moodIncrease = action switch
        {
            WaifuAction.Hug => baseMood,
            WaifuAction.Kiss => (int)(baseMood * 1.5),
            WaifuAction.Pat => (int)(baseMood * 0.75),
            _ => baseMood
        };

        await using var ctx = db.GetDbContext();

        var updated = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuId)
            .Set(x => x.Mood, x => x.Mood + moodIncrease > 1000 ? 1000 : x.Mood + moodIncrease)
            .UpdateWithOutputAsync((o, n) => n.Mood);

        if (updated.Length == 0)
            return new ErrWaifuNotFound();

        await cache.AddAsync(actionsKey, used + 1, TimeSpan.FromHours(24));

        return new Success<int>(moodIncrease);
    }

    #endregion

    #region Gift Actions

    /// <summary>
    /// Gives a gift to a waifu, increasing their stats.
    /// </summary>
    public async Task<GiftResult> GiftAsync(ulong fromUserId, ulong waifuUserId, string itemName, int count = 1)
    {
        if (fromUserId == waifuUserId)
            return new ErrSelfNotAllowed();

        count = Math.Clamp(count, 1, configService.Data.MaxGiftCount);

        var item = WaifuGiftItems.FindTodaysItem(GetAllItems(), itemName);
        if (item is null)
            return new ErrItemNotAvailable();

        var totalCost = item.Price * count;

        if (!await cs.RemoveAsync(fromUserId, totalCost, new("waifu", "gift")))
            return new ErrInsufficientFunds();

        await using var ctx = db.GetDbContext();

        var totalEffect = item.Effect * count;
        int updated;

        if (item.Type == GiftItemType.Food)
        {
            updated = await ctx.GetTable<WaifuInfo>()
                .Where(x => x.UserId == waifuUserId)
                .Set(x => x.Food, x => x.Food + totalEffect > 1000 ? 1000 : x.Food + totalEffect)
                .UpdateAsync();
        }
        else
        {
            updated = await ctx.GetTable<WaifuInfo>()
                .Where(x => x.UserId == waifuUserId)
                .Set(x => x.Mood, x => x.Mood + totalEffect > 1000 ? 1000 : x.Mood + totalEffect)
                .UpdateAsync();
        }

        if (updated == 0)
        {
            await cs.AddAsync(fromUserId, totalCost, new("waifu", "gift-refund"));
            return new ErrWaifuNotFound();
        }

        // Persist gift count
        await ctx.GetTable<WaifuGiftCount>()
            .InsertOrUpdateAsync(() => new()
                {
                    WaifuUserId = waifuUserId,
                    GiftItemId = item.Id,
                    Count = count
                },
                old => new()
                {
                    Count = old.Count + count
                },
                () => new()
                {
                    WaifuUserId = waifuUserId,
                    GiftItemId = item.Id
                });

        if (quests is not null)
            await quests.ReportActionAsync(fromUserId, QuestEventType.WaifuGiftSent);

        return new Success<WaifuGiftItem>(item);
    }

    /// <summary>
    /// Gets all gift counts for a waifu, resolved to gift items.
    /// </summary>
    public async Task<List<(WaifuGiftItem Item, int Count)>> GetGiftCountsAsync(ulong waifuUserId)
    {
        await using var ctx = db.GetDbContext();

        var rows = await ctx.GetTable<WaifuGiftCount>()
            .Where(x => x.WaifuUserId == waifuUserId && x.Count > 0)
            .ToListAsyncLinqToDB();

        var result = new List<(WaifuGiftItem, int)>();

        foreach (var row in rows)
        {
            var item = WaifuGiftItems.FindItemById(GetAllItems(), row.GiftItemId);
            if (item is not null)
                result.Add((item, row.Count));
        }

        return result.OrderByDescending(x => x.Item1.Price).ToList();
    }

    #endregion

    #region Pending Payouts

    /// <summary>
    /// Adds an amount to a user's pending payout (upsert).
    /// </summary>
    internal async Task AddPendingPayoutInternalAsync(NadekoContext ctx, ulong userId, decimal amount)
    {
        await ctx.GetTable<WaifuPendingPayout>()
            .InsertOrUpdateAsync(
                () => new () { UserId = userId, Amount = amount },
                old => new() { Amount = old.Amount + amount },
                () => new () { UserId = userId });
    }

    /// <summary>
    /// Gets the pending payout amount for a user (floored to whole units).
    /// </summary>
    public async Task<long> GetPendingPayoutAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();
        var amount = await ctx.GetTable<WaifuPendingPayout>()
            .Where(x => x.UserId == userId)
            .Select(x => x.Amount)
            .FirstOrDefaultAsyncLinqToDB();

        return (long)Math.Floor(amount);
    }

    /// <summary>
    /// Claims the pending payout. Floors to whole units, pays to wallet, discards fractional remainder.
    /// </summary>
    public async Task<ClaimPayoutResult> ClaimPayoutAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();

        var amounts = await ctx.GetTable<WaifuPendingPayout>()
            .Where(x => x.UserId == userId && x.Amount >= 1)
            .DeleteWithOutputAsync(x => x.Amount);

        if (amounts.Length == 0)
            return new ErrNoPendingPayout();

        var claimable = (long)Math.Floor(amounts[0]);
        await cs.AddAsync(userId, claimable, new("waifu", "payout-claim"));

        return new Success<long>(claimable);
    }

    #endregion

    #region Background Jobs

    private async Task DecayLoopAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(2));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await DecayInternalAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error decaying waifu stats");
            }
        }
    }

    /// <summary>
    /// Decrements Mood and Food by 1 for all waifus whose LastDecayTime is more than 2 hours ago.
    /// </summary>
    internal async Task DecayInternalAsync()
    {
        await using var ctx = db.GetDbContext();
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-2);

        await ctx.GetTable<WaifuInfo>()
            .Where(x => x.LastDecayTime < cutoff)
            .Set(x => x.Mood, x => x.Mood > 0 ? x.Mood - 1 : 0)
            .Set(x => x.Food, x => x.Food > 0 ? x.Food - 1 : 0)
            .Set(x => x.LastDecayTime, _timeProvider.GetUtcNow().UtcDateTime)
            .UpdateAsync();
    }

    private async Task CycleLoopAsync()
    {
        await CatchUpMissedCyclesInternalAsync();

        while (true)
        {
            try
            {
                var currentCycle = GetCurrentCycle();
                var cycleEnd = GetCycleStartTime(currentCycle + 1);

                Log.Information("Waifu cycle #{Cycle} started. Snapshotting. Next payout at {CycleEnd}",
                    currentCycle, cycleEnd);

                await SnapshotCycleInternalAsync(currentCycle);

                var delay = cycleEnd - _timeProvider.GetUtcNow().UtcDateTime;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay);

                await using var ctx = db.GetDbContext();
                await ProcessCycleInternalAsync(ctx, currentCycle);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in waifu cycle loop");
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }

    /// <summary>
    /// Processes any past cycles that were snapshotted but never paid out (e.g. bot was down at cycle boundary).
    /// </summary>
    internal async Task CatchUpMissedCyclesInternalAsync()
    {
        await using var ctx = db.GetDbContext();
        var currentCycle = GetCurrentCycle();

        var missedCycles = await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.CycleNumber < currentCycle
                        && !ctx.GetTable<WaifuCycle>()
                            .Any(wc => wc.CycleNumber == x.CycleNumber
                                       && wc.WaifuUserId == x.WaifuUserId))
            .Select(x => x.CycleNumber)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsyncLinqToDB();

        foreach (var cycle in missedCycles)
        {
            Log.Warning("Catching up missed waifu cycle #{Cycle}", cycle);
            await ProcessCycleInternalAsync(ctx, cycle);
        }
    }

    /// <summary>
    /// Takes a snapshot of fan lists and bank balances for a cycle.
    /// Called once at cycle start. If rows already exist (bot restarted mid-cycle), skips.
    /// </summary>
    internal async Task SnapshotCycleInternalAsync(int cycleNumber)
    {
        await using var ctx = db.GetDbContext();

        // Get waifus that already have snapshots for this cycle
        var alreadySnapshotted = await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.CycleNumber == cycleNumber)
            .Select(x => x.WaifuUserId)
            .Distinct()
            .ToListAsyncLinqToDB();

        var alreadySet = alreadySnapshotted.ToHashSet();

        // Get all fans with their bank balances in one join query
        var fanBalances = await ctx.GetTable<WaifuFan>()
            .LeftJoin(ctx.GetTable<BankUser>(),
                (f, b) => f.UserId == b.UserId,
                (f, b) => new
                {
                    f.WaifuUserId,
                    FanUserId = f.UserId,
                    Balance = b != null ? b.Balance : 0L
                })
            .ToListAsyncLinqToDB();

        foreach (var fb in fanBalances)
        {
            if (alreadySet.Contains(fb.WaifuUserId))
                continue;

            await ctx.GetTable<WaifuCycleSnapshot>()
                .InsertAsync(() => new()
                {
                    CycleNumber = cycleNumber,
                    WaifuUserId = fb.WaifuUserId,
                    UserId = fb.FanUserId,
                    SnapshotBalance = fb.Balance
                });
        }
    }

    internal async Task ProcessCycleInternalAsync(NadekoContext ctx, int cycleNumber)
    {
        var conf = configService.Data;
        var nextCycle = cycleNumber + 1;
        var nextCycleStart = GetCycleStartTime(nextCycle);

        Log.Information("Processing waifu cycle #{Cycle}. Next cycle #{NextCycle} starts at {NextStart}",
            cycleNumber, nextCycle, nextCycleStart);

        var waifus = await ctx.GetTable<WaifuInfo>()
            .ToListAsyncLinqToDB();

        foreach (var waifu in waifus)
        {
            try
            {
                var alreadyProcessed = await ctx.GetTable<WaifuCycle>()
                    .AnyAsyncLinqToDB(x => x.WaifuUserId == waifu.UserId && x.CycleNumber == cycleNumber);

                if (alreadyProcessed)
                    continue;

                if (waifu.ManagerUserId is null)
                {
                    var decay = conf.ManagerlessDecayPercent / 100.0;
                    var newPrice = Math.Max(conf.MinPrice, (long)(waifu.Price * (1 - decay)));
                    if (newPrice != waifu.Price)
                    {
                        await ctx.GetTable<WaifuInfo>()
                            .Where(x => x.Id == waifu.Id)
                            .Set(x => x.Price, newPrice)
                            .UpdateAsync();
                    }

                    Log.Information("Waifu {UserId} has no manager. Price decayed {OldPrice} -> {NewPrice}",
                        waifu.UserId, waifu.Price, newPrice);

                    await ctx.GetTable<WaifuCycle>()
                        .InsertAsync(() => new()
                        {
                            WaifuUserId = waifu.UserId,
                            CycleNumber = cycleNumber,
                            ManagerUserId = 0,
                            TotalBacked = 0,
                            TotalReturns = 0,
                            WaifuEarnings = 0,
                            ManagerEarnings = 0,
                            FanPool = 0,
                            MoodSnapshot = waifu.Mood,
                            FoodSnapshot = waifu.Food,
                            ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime
                        });

                    continue;
                }

                await ProcessWaifuCycleInternalAsync(ctx, waifu, cycleNumber);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing cycle {Cycle} for waifu {UserId}", cycleNumber, waifu.UserId);
            }
        }

        Log.Information("Waifu cycle #{Cycle} processing complete", cycleNumber);

        await CleanupOldCycleDataInternalAsync(ctx, cycleNumber);
    }

    /// <summary>
    /// Deletes snapshot and cycle records older than 10 cycles.
    /// </summary>
    internal async Task CleanupOldCycleDataInternalAsync(NadekoContext ctx, int currentCycle)
    {
        var cutoff = currentCycle - 10;
        if (cutoff < 0)
            return;

        var deletedSnapshots = await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.CycleNumber < cutoff)
            .DeleteAsync();

        var deletedCycles = await ctx.GetTable<WaifuCycle>()
            .Where(x => x.CycleNumber < cutoff)
            .DeleteAsync();

        if (deletedSnapshots > 0 || deletedCycles > 0)
        {
            Log.Information("Cleaned up old cycle data: {Snapshots} snapshots, {Cycles} cycle records (before cycle #{Cutoff})",
                deletedSnapshots, deletedCycles, cutoff);
        }
    }

    internal async Task ProcessWaifuCycleInternalAsync(NadekoContext ctx, WaifuInfo waifu, int cycleNumber)
    {
        var snapshotRows = await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.WaifuUserId == waifu.UserId && x.CycleNumber == cycleNumber)
            .ToListAsyncLinqToDB();

        if (snapshotRows.Count == 0)
        {
            Log.Information("Waifu {UserId} cycle #{Cycle}: no snapshot rows, skipping", waifu.UserId, cycleNumber);
            return;
        }

        var snapshots = snapshotRows.Select(x => (x.UserId, Balance: x.SnapshotBalance)).ToList();
        var totalBacked = snapshots.Sum(x => x.Balance);

        if (totalBacked <= 0)
        {
            Log.Information("Waifu {UserId} cycle #{Cycle}: {FanCount} fans but 0 total backed, skipping",
                waifu.UserId, cycleNumber, snapshots.Count);
            return;
        }

        var managerId = waifu.ManagerUserId!.Value;

        var effectiveRate = BaseReturnRate;
        var cycleRate = effectiveRate / CyclesPerYear;
        var statMultiplier = (waifu.Mood + waifu.Food) / 2000.0;
        var cappedBacked = Math.Min(totalBacked, waifu.ReturnsCap);

        var returnsD = (decimal)(cappedBacked * cycleRate * statMultiplier);
        if (returnsD < 1m)
        {
            Log.Information("Waifu {UserId} cycle #{Cycle}: returns<1 (mood={Mood}, food={Food}, backed={Backed})",
                waifu.UserId, cycleNumber, waifu.Mood, waifu.Food, totalBacked);
            return;
        }

        var waifuCutD = returnsD * waifu.WaifuFeePercent / 100m;
        var managerCutD = waifuCutD * (decimal)configService.Data.ManagerCutPercent;
        var waifuNetD = waifuCutD - managerCutD;
        var fanPoolD = returnsD - waifuCutD;

        var returns = (long)returnsD;
        var waifuCut = (long)waifuCutD;
        var managerCut = (long)managerCutD;
        var waifuNet = (long)waifuNetD;
        var fanPool = (long)fanPoolD;

        Log.Information(
            "Waifu {UserId} cycle #{Cycle}: backed={TotalBacked} capped={Capped} rate={Rate:P2} stats={Stats:P0} returns={Returns} | waifu={WaifuNet} manager={ManagerCut} fanPool={FanPool} fee={Fee}%",
            waifu.UserId, cycleNumber, totalBacked, cappedBacked, effectiveRate, statMultiplier,
            returns, waifuNet, managerCut, fanPool, waifu.WaifuFeePercent);

        if (waifuNetD > 0)
            await AddPendingPayoutInternalAsync(ctx, waifu.UserId, waifuNetD);

        var managerFanShareD = 0m;
        var managerSnapshot = snapshots.FirstOrDefault(x => x.UserId == managerId);
        if (managerSnapshot.Balance > 0 && totalBacked > 0)
            managerFanShareD = fanPoolD * managerSnapshot.Balance / totalBacked;

        var totalManagerPayD = managerCutD + managerFanShareD;
        if (totalManagerPayD > 0)
            await AddPendingPayoutInternalAsync(ctx, managerId, totalManagerPayD);

        Log.Information("  Manager {ManagerId}: cut={ManagerCut} + fanShare={FanShare} = {Total}",
            managerId, (long)managerCutD, (long)managerFanShareD, (long)totalManagerPayD);

        foreach (var (fanId, balance) in snapshots)
        {
            if (fanId == managerId)
                continue;

            var shareD = fanPoolD * balance / totalBacked;
            if (shareD > 0)
                await AddPendingPayoutInternalAsync(ctx, fanId, shareD);

            Log.Information("  Fan {FanId}: balance={Balance} share={Share}",
                fanId, balance, (long)shareD);
        }

        await ctx.GetTable<WaifuCycle>()
            .InsertAsync(() => new()
            {
                WaifuUserId = waifu.UserId,
                CycleNumber = cycleNumber,
                ManagerUserId = managerId,
                TotalBacked = totalBacked,
                TotalReturns = returns,
                WaifuEarnings = waifuNet,
                ManagerEarnings = managerCut,
                FanPool = fanPool,
                MoodSnapshot = waifu.Mood,
                FoodSnapshot = waifu.Food,
                ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime
            });

        await ctx.GetTable<WaifuInfo>()
            .Where(x => x.Id == waifu.Id)
            .Set(x => x.TotalProduced, x => x.TotalProduced + returns)
            .UpdateAsync();
    }

    #endregion
}

#region DTOs

/// <summary>
/// DTO for waifu info display.
/// </summary>
public sealed class WaifuInfoDto
{
    public ulong UserId { get; init; }
    public int Mood { get; init; }
    public int Food { get; init; }
    public int WaifuFeePercent { get; init; }
    public long Price { get; init; }
    public long TotalProduced { get; init; }
    public long ReturnsCap { get; init; }

    /// <summary>
    /// Number of fans currently in WaifuFan table (active).
    /// </summary>
    public int FanCount { get; init; }

    /// <summary>
    /// Total backed amount from the current cycle's snapshot (locked at cycle start).
    /// Falls back to 0 if no snapshot exists yet.
    /// </summary>
    public long SnapshotTotalBacked { get; init; }

    /// <summary>
    /// Total returns from the last completed cycle.
    /// </summary>
    public long LastCycleReturns { get; init; }

    /// <summary>
    /// Number of fans who joined after the current cycle's snapshot was taken.
    /// </summary>
    public int PendingJoins { get; init; }

    /// <summary>
    /// Number of fans who left after the current cycle's snapshot was taken.
    /// </summary>
    public int PendingLeaves { get; init; }

    public ulong? ManagerId { get; init; }
    public bool IsHubby { get; init; }
    public string? CustomAvatarUrl { get; init; }
    public string? Description { get; init; }
    public string? Quote { get; init; }
}

/// <summary>
/// DTO for projected cycle payout.
/// </summary>
public sealed class PayoutProjectionDto
{
    public long TotalReturns { get; init; }
    public long WaifuCut { get; init; }
    public long ManagerCut { get; init; }
    public long FanPool { get; init; }
    public double EffectiveRate { get; init; }
}

/// <summary>
/// DTO for fan info.
/// </summary>
public sealed class FanInfoDto
{
    public ulong UserId { get; init; }

    /// <summary>
    /// Earnings from the last completed cycle (0 if no history).
    /// </summary>
    public long LastCycleEarnings { get; init; }

    /// <summary>
    /// Whether this fan joined after the current cycle's snapshot (pending for next cycle).
    /// </summary>
    public bool IsPending { get; init; }

    public DateTime BackedAt { get; init; }
}

/// <summary>
/// DTO for leaderboard entry.
/// </summary>
public sealed class LeaderboardEntryDto
{
    public ulong UserId { get; init; }
    public string? Username { get; init; }
    public long Price { get; init; }
    public int Mood { get; init; }
    public int Food { get; init; }
    public long ReturnsCap { get; init; }
    public double RealizedReturnRate { get; init; }
    public long TotalReturns { get; init; }
    public long AvgBacked { get; init; }
    public int CyclesActive { get; init; }
    public long SnapshotTotalBacked { get; init; }
    public bool HasManager { get; init; }
}

/// <summary>
/// DTO for a waifu managed by the user.
/// </summary>
public sealed class ManagedWaifuDto
{
    public ulong WaifuUserId { get; init; }
    public string? Username { get; init; }
    public long Price { get; init; }
    public long TotalBacked { get; init; }
}

/// <summary>
/// Sort order for the waifu leaderboard.
/// </summary>
public enum WaifuLbOrder
{
    Backing,
    Price
}

#endregion
