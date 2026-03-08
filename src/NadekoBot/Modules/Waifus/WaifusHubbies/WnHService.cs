using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Waifus.WaifusHubbies.Db;
using NadekoBot.Modules.Games.Quests;
using OneOf;
using OneOf.Types;

namespace NadekoBot.Modules.Waifus.WaifusHubbies;

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
public readonly struct ErrIsManager;
public readonly struct ErrNotFanOfTarget;
public readonly struct ErrPriceTooLow;

[GenerateOneOf]
public sealed partial class OptInResult : OneOfBase<ErrAlreadyOptedIn, ErrInsufficientFunds, Success>;

[GenerateOneOf]
public sealed partial class OptOutResult : OneOfBase<ErrNotOptedIn, ErrHasFans, Success>;

[GenerateOneOf]
public sealed partial class BackResult : OneOfBase<ErrAlreadyBacking, ErrWaifuNotFound, ErrIsManager, Success>;

[GenerateOneOf]
public sealed partial class StopBackingResult : OneOfBase<ErrNotBacking, Success>;

[GenerateOneOf]
public sealed partial class ImproveMoodResult : OneOfBase<ErrSelfNotAllowed, ErrNoActionsLeft, ErrWaifuNotFound, Success>;

[GenerateOneOf]
public sealed partial class SetFeeResult : OneOfBase<ErrWaifuNotFound, ErrNotOptedIn, ErrInvalidPercent, Success>;

public readonly struct ErrItemNotAvailable;

[GenerateOneOf]
public sealed partial class GiftResult : OneOfBase<ErrSelfNotAllowed, ErrInsufficientFunds, ErrWaifuNotFound, ErrItemNotAvailable, Success<WaifuGiftItem>>;

[GenerateOneOf]
public sealed partial class BuyManagerResult : OneOfBase<ErrNotFanOfTarget, ErrOutsideBuyWindow, ErrInsufficientFunds, ErrWaifuNotFound, ErrPriceTooLow, Success<BuyManagerInfo>>;

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
public sealed class WnHService(
    DbService db,
    IBotCache cache,
    ICurrencyService cs,
    DiscordSocketClient client,
    QuestService? quests = null,
    TimeProvider? timeProvider = null
) : INService, IReadyExecutor
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private const int BASE_MOOD_INCREASE = 50;
    private const int MAX_DAILY_ACTIONS = 2;
    private const int MAX_GIFT_COUNT = 100;
    private const long OPT_IN_COST = 10_000;
    private const long DEFAULT_INITIAL_PRICE = OPT_IN_COST / 10;
    private const long MIN_WAIFU_PRICE = 1_000;
    private const double MANAGER_BUY_PREMIUM = 0.15;
    private const double MANAGER_OLD_PAYOUT = 0.90;
    private const double MANAGER_WAIFU_PAYOUT = 0.05;
    private const double MANAGER_BURN = 0.05;
    private const double MANAGER_EXIT_REFUND = 0.50;
    private const double MANAGER_EXIT_WAIFU = 0.10;
    private const double MANAGER_EXIT_FANS = 0.35;
    private const double MANAGER_EXIT_BURN = 0.05;
    public const double MANAGERLESS_PRICE_DECAY = 0.10;
    private const double MANAGER_CUT_PERCENT = 0.10;
    private const double BASE_RETURN_RATE = 0.17;
    private const double MAX_RETURN_RATE = 0.20;
    private const long DEFAULT_RETURNS_CAP = 1_000_000;
    private const int BUY_WINDOW_HOURS = 18;
#if DEBUG
    private const double CYCLE_HOURS = 5.0 / 60.0; // 5 minutes for testing
#else
    private const double CYCLE_HOURS = 84.0; // 3.5 days
#endif
    private static readonly double CYCLES_PER_YEAR = 365.25 * 24 / CYCLE_HOURS;

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
        => (int)((_timeProvider.GetUtcNow().UtcDateTime - Epoch).TotalHours / CYCLE_HOURS);

    /// <summary>
    /// Gets when a cycle starts.
    /// </summary>
    public DateTime GetCycleStartTime(int? cycle = null)
    {
        var c = cycle ?? GetCurrentCycle();
        return Epoch.AddHours(c * CYCLE_HOURS);
    }

    /// <summary>
    /// Gets when the next cycle starts.
    /// </summary>
    public DateTime GetNextCycleTime()
        => GetCycleStartTime(GetCurrentCycle() + 1);

    /// <summary>
    /// Checks if we're within the buy window (first 18h of cycle).
    /// </summary>
    public bool IsWithinBuyWindow()
        => _timeProvider.GetUtcNow().UtcDateTime < GetCycleStartTime().AddHours(BUY_WINDOW_HOURS);

    /// <summary>
    /// Gets the fraction of the current cycle that has elapsed (0.0 to 1.0).
    /// </summary>
    public double GetCycleProgressFraction()
    {
        var start = GetCycleStartTime();
        var elapsed = (_timeProvider.GetUtcNow().UtcDateTime - start).TotalHours;
        return Math.Clamp(elapsed / CYCLE_HOURS, 0.0, 1.0);
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
            .Where(x => x.WaifuUserId == userId && x.LeftAt == null)
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
            .Where(x => x.UserId == userId && x.LeftAt == null)
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
            .AnyAsyncLinqToDB(x => x.UserId == userId && x.LeftAt == null);
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
    /// Gets the manager exit info for confirmation display.
    /// </summary>
    public async Task<ManagerExitInfo?> GetManagerExitInfoAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.ManagerUserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return null;

        var price = wi.Price;
        return new ManagerExitInfo
        {
            Refund = (long)(price * MANAGER_EXIT_REFUND),
            WaifuCut = (long)(price * MANAGER_EXIT_WAIFU),
            FanDistribution = (long)(price * MANAGER_EXIT_FANS),
            Burned = (long)(price * MANAGER_EXIT_BURN),
            NewPrice = (long)(price * MANAGER_EXIT_REFUND)
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
            .Where(x => x.WaifuUserId == waifuUserId && x.LeftAt == null)
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
        return MAX_DAILY_ACTIONS - (actionsUsed.TryPickT0(out var used, out _) ? used : 0);
    }

    /// <summary>
    /// Gets leaderboard ranked by realized return rate over last 8 cycles.
    /// </summary>
    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int count = 10)
    {
        await using var ctx = db.GetDbContext();
        var currentCycle = GetCurrentCycle();
        var startCycle = currentCycle - 8;

        var waifus = await ctx.GetTable<WaifuInfo>()
            .ToListAsyncLinqToDB();

        var entries = new List<LeaderboardEntryDto>();

        foreach (var waifu in waifus)
        {
            var cycles = await ctx.GetTable<WaifuCycle>()
                .Where(x => x.WaifuUserId == waifu.UserId && x.CycleNumber > startCycle)
                .ToListAsyncLinqToDB();

            if (cycles.Count == 0)
                continue;

            var totalReturns = cycles.Sum(x => x.TotalReturns);
            var avgBacked = cycles.Average(x => (double)x.TotalBacked);

            var realizedRate = avgBacked > 0
                ? (totalReturns / avgBacked) * CYCLES_PER_YEAR
                : 0;

            entries.Add(new LeaderboardEntryDto
            {
                UserId = waifu.UserId,
                RealizedReturnRate = realizedRate,
                TotalReturns = totalReturns,
                AvgBacked = (long)avgBacked,
                CyclesActive = cycles.Count
            });
        }

        return entries
            .OrderByDescending(x => x.RealizedReturnRate)
            .Take(count)
            .ToList();
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
                .Where(x => x.WaifuUserId == waifuUserId && x.LeftAt == null)
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

        var effectiveRate = Math.Min(BASE_RETURN_RATE, MAX_RETURN_RATE);
        var cycleRate = effectiveRate / CYCLES_PER_YEAR;
        var statMultiplier = (wi.Mood + wi.Food) / 2000.0;
        var cappedBacked = Math.Min(totalBacked, wi.ReturnsCap);
        var returns = (long)(cappedBacked * cycleRate * statMultiplier);

        if (returns <= 0)
            return new PayoutProjectionDto { EffectiveRate = effectiveRate };

        var waifuCut = (long)(returns * wi.WaifuFeePercent / 100.0);
        var managerCut = (long)(waifuCut * MANAGER_CUT_PERCENT);
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

        var existing = await ctx.GetTable<WaifuInfo>()
            .AnyAsyncLinqToDB(x => x.UserId == userId);

        if (existing)
            return new ErrAlreadyOptedIn();

        if (!await cs.RemoveAsync(userId, OPT_IN_COST, new("waifu", "opt-in")))
            return new ErrInsufficientFunds();

        await ctx.GetTable<WaifuInfo>()
            .InsertAsync(() => new WaifuInfo
            {
                UserId = userId,
                IsHubby = isHubby,
                Mood = 500,
                Food = 500,
                WaifuFeePercent = 5,
                Price = DEFAULT_INITIAL_PRICE,
                ReturnsCap = DEFAULT_RETURNS_CAP,
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
            .AnyAsyncLinqToDB(x => x.WaifuUserId == userId && x.LeftAt == null);

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
            .Where(x => x.UserId == userId && x.LeftAt == null)
            .FirstOrDefaultAsyncLinqToDB();

        if (existingFan is not null)
        {
            if (existingFan.WaifuUserId == waifuUserId)
                return new ErrAlreadyBacking();

            var isManager = await ctx.GetTable<WaifuInfo>()
                .AnyAsyncLinqToDB(x => x.ManagerUserId == userId);

            if (isManager)
                return new ErrIsManager();

            // Switch: hard-delete old fan rows (including any soft-deleted), insert new one below
            await ctx.GetTable<WaifuFan>()
                .Where(x => x.UserId == userId)
                .DeleteAsync();
        }
        else
        {
            // Clean up any leftover soft-deleted rows for this user
            await ctx.GetTable<WaifuFan>()
                .Where(x => x.UserId == userId && x.LeftAt != null)
                .DeleteAsync();
        }

        var waifuExists = await ctx.GetTable<WaifuInfo>()
            .AnyAsyncLinqToDB(x => x.UserId == waifuUserId);

        if (!waifuExists)
            return new ErrWaifuNotFound();

        await ctx.GetTable<WaifuFan>()
            .InsertAsync(() => new WaifuFan
            {
                UserId = userId,
                WaifuUserId = waifuUserId,
                DelegatedAt = _timeProvider.GetUtcNow().UtcDateTime
            });

        return new Success();
    }

    /// <summary>
    /// Stops being a fan. If the user is a manager, triggers the manager exit flow.
    /// </summary>
    public async Task<StopBackingResult> StopBeingFanAsync(ulong userId)
    {
        await using var ctx = db.GetDbContext();

        var fan = await ctx.GetTable<WaifuFan>()
            .Where(x => x.UserId == userId && x.LeftAt == null)
            .FirstOrDefaultAsyncLinqToDB();

        if (fan is null)
            return new ErrNotBacking();

        // Check if user is a manager
        var managedWaifu = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.ManagerUserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (managedWaifu is not null)
        {
            await ProcessManagerExitAsync(ctx, managedWaifu, userId);
        }

        await ctx.GetTable<WaifuFan>()
            .Where(x => x.Id == fan.Id)
            .Set(x => x.LeftAt, _timeProvider.GetUtcNow().UtcDateTime)
            .UpdateAsync();

        return new Success();
    }

    /// <summary>
    /// Processes the financial consequences of a manager voluntarily exiting.
    /// </summary>
    private async Task ProcessManagerExitAsync(NadekoContext ctx, WaifuInfo waifu, ulong managerUserId)
    {
        var price = waifu.Price;
        var refund = (long)(price * MANAGER_EXIT_REFUND);
        var waifuCut = (long)(price * MANAGER_EXIT_WAIFU);
        var fanDistribution = (long)(price * MANAGER_EXIT_FANS);

        // Refund manager
        if (refund > 0)
            await cs.AddAsync(managerUserId, refund, new("waifu", "manager-exit-refund"));

        // Pay waifu
        if (waifuCut > 0)
            await cs.AddAsync(waifu.UserId, waifuCut, new("waifu", "manager-exit-waifu"));

        // Distribute to other fans
        if (fanDistribution > 0)
        {
            var otherFans = await ctx.GetTable<WaifuFan>()
                .Where(x => x.WaifuUserId == waifu.UserId && x.UserId != managerUserId && x.LeftAt == null)
                .Select(x => x.UserId)
                .ToListAsyncLinqToDB();

            if (otherFans.Count > 0)
            {
                long totalOtherBacked = 0;
                var fanBalances = new List<(ulong UserId, long Balance)>();

                foreach (var fanId in otherFans)
                {
                    var balance = await ctx.GetTable<BankUser>()
                        .Where(x => x.UserId == fanId)
                        .Select(x => x.Balance)
                        .FirstOrDefaultAsyncLinqToDB();

                    fanBalances.Add((fanId, balance));
                    totalOtherBacked += balance;
                }

                if (totalOtherBacked > 0)
                {
                    foreach (var (fanId, balance) in fanBalances)
                    {
                        var share = (long)(fanDistribution * ((double)balance / totalOtherBacked));
                        if (share > 0)
                            await cs.AddAsync(fanId, share, new("waifu", "manager-exit-dist"));
                    }
                }
            }
        }

        // Drop price and remove manager
        var newPrice = Math.Max(MIN_WAIFU_PRICE, refund);
        await ctx.GetTable<WaifuInfo>()
            .Where(x => x.Id == waifu.Id)
            .Set(x => x.ManagerUserId, (ulong?)null)
            .Set(x => x.Price, newPrice)
            .UpdateAsync();
    }

    #endregion

    #region Manager Purchase

    /// <summary>
    /// Buys the manager position for a waifu.
    /// </summary>
    public async Task<BuyManagerResult> BuyManagerAsync(ulong buyerUserId, ulong waifuUserId)
    {
        await using var ctx = db.GetDbContext();

        // Must be a fan of the target waifu
        var fan = await ctx.GetTable<WaifuFan>()
            .Where(x => x.UserId == buyerUserId && x.WaifuUserId == waifuUserId && x.LeftAt == null)
            .FirstOrDefaultAsyncLinqToDB();

        if (fan is null)
            return new ErrNotFanOfTarget();

        if (!IsWithinBuyWindow())
            return new ErrOutsideBuyWindow();

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuUserId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return new ErrWaifuNotFound();

        var requiredPrice = (long)Math.Ceiling(wi.Price * (1 + MANAGER_BUY_PREMIUM));

        if (!await cs.RemoveAsync(buyerUserId, requiredPrice, new("waifu", "manager-buy")))
            return new ErrInsufficientFunds();

        var oldManagerId = wi.ManagerUserId;
        var oldManagerPayout = (long)(requiredPrice * MANAGER_OLD_PAYOUT);
        var waifuPayout = (long)(requiredPrice * MANAGER_WAIFU_PAYOUT);
        var burned = requiredPrice - oldManagerPayout - waifuPayout;

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

        // Update waifu with new manager and price
        await ctx.GetTable<WaifuInfo>()
            .Where(x => x.Id == wi.Id)
            .Set(x => x.ManagerUserId, buyerUserId)
            .Set(x => x.Price, requiredPrice)
            .UpdateAsync();

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

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuUserId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return new ErrNotOptedIn();

        await ctx.GetTable<WaifuInfo>()
            .Where(x => x.Id == wi.Id)
            .Set(x => x.WaifuFeePercent, percent)
            .UpdateAsync();

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

        var actionsKey = GetActionsKey(userId);
        var actionsUsed = await cache.GetAsync(actionsKey);
        var used = actionsUsed.TryPickT0(out var u, out _) ? u : 0;

        if (used >= MAX_DAILY_ACTIONS)
            return new ErrNoActionsLeft();

        await using var ctx = db.GetDbContext();

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
            return new ErrWaifuNotFound();

        var moodIncrease = action switch
        {
            WaifuAction.Hug => BASE_MOOD_INCREASE,
            WaifuAction.Kiss => (int)(BASE_MOOD_INCREASE * 1.5),
            WaifuAction.Pat => (int)(BASE_MOOD_INCREASE * 0.75),
            _ => BASE_MOOD_INCREASE
        };

        var newMood = Math.Min(1000, wi.Mood + moodIncrease);

        await ctx.GetTable<WaifuInfo>()
            .Where(x => x.Id == wi.Id)
            .Set(x => x.Mood, newMood)
            .UpdateAsync();

        await cache.AddAsync(actionsKey, used + 1, TimeSpan.FromHours(24));

        return new Success();
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

        count = Math.Clamp(count, 1, MAX_GIFT_COUNT);

        var item = WaifuGiftItems.FindTodaysItem(itemName);
        if (item is null)
            return new ErrItemNotAvailable();

        var totalCost = item.Price * count;

        if (!await cs.RemoveAsync(fromUserId, totalCost, new("waifu", "gift")))
            return new ErrInsufficientFunds();

        await using var ctx = db.GetDbContext();

        var wi = await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == waifuUserId)
            .FirstOrDefaultAsyncLinqToDB();

        if (wi is null)
        {
            await cs.AddAsync(fromUserId, totalCost, new("waifu", "gift-refund"));
            return new ErrWaifuNotFound();
        }

        var totalEffect = item.Effect * count;

        if (item.Type == GiftItemType.Food)
        {
            var newFood = Math.Min(1000, wi.Food + totalEffect);
            await ctx.GetTable<WaifuInfo>()
                .Where(x => x.Id == wi.Id)
                .Set(x => x.Food, newFood)
                .UpdateAsync();
        }
        else
        {
            var newMood = Math.Min(1000, wi.Mood + totalEffect);
            await ctx.GetTable<WaifuInfo>()
                .Where(x => x.Id == wi.Id)
                .Set(x => x.Mood, newMood)
                .UpdateAsync();
        }

        // Persist gift count
        var existing = await ctx.GetTable<WaifuGiftCount>()
            .FirstOrDefaultAsyncLinqToDB(x => x.WaifuUserId == waifuUserId && x.GiftItemId == item.Id);

        if (existing is not null)
        {
            await ctx.GetTable<WaifuGiftCount>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.Count, existing.Count + count)
                .UpdateAsync();
        }
        else
        {
            await ctx.GetTable<WaifuGiftCount>()
                .InsertAsync(() => new WaifuGiftCount
                {
                    WaifuUserId = waifuUserId,
                    GiftItemId = item.Id,
                    Count = count
                });
        }

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
            var item = WaifuGiftItems.AllItems.FirstOrDefault(x => x.Id == row.GiftItemId);
            if (item is not null)
                result.Add((item, row.Count));
        }

        return result.OrderByDescending(x => x.Item1.Price).ToList();
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
    /// Takes a snapshot of fan lists and bank balances for a cycle.
    /// Called once at cycle start. If rows already exist (bot restarted mid-cycle), skips.
    /// </summary>
    internal async Task SnapshotCycleInternalAsync(int cycleNumber)
    {
        await using var ctx = db.GetDbContext();
        var cycleStartTime = GetCycleStartTime(cycleNumber);

        var waifus = await ctx.GetTable<WaifuInfo>()
            .ToListAsyncLinqToDB();

        foreach (var waifu in waifus)
        {
            var alreadySnapshotted = await ctx.GetTable<WaifuCycleSnapshot>()
                .AnyAsyncLinqToDB(x => x.WaifuUserId == waifu.UserId && x.CycleNumber == cycleNumber);

            if (alreadySnapshotted)
                continue;

            var fans = await ctx.GetTable<WaifuFan>()
                .Where(x => x.WaifuUserId == waifu.UserId
                            && x.LeftAt == null)
                .ToListAsyncLinqToDB();

            foreach (var fan in fans)
            {
                var balance = await ctx.GetTable<BankUser>()
                    .Where(x => x.UserId == fan.UserId)
                    .Select(x => x.Balance)
                    .FirstOrDefaultAsyncLinqToDB();

                await ctx.GetTable<WaifuCycleSnapshot>()
                    .InsertAsync(() => new WaifuCycleSnapshot
                    {
                        CycleNumber = cycleNumber,
                        WaifuUserId = waifu.UserId,
                        UserId = fan.UserId,
                        SnapshotBalance = balance
                    });
            }
        }
    }

    internal async Task ProcessCycleInternalAsync(NadekoContext ctx, int cycleNumber)
    {
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
                    var newPrice = Math.Max(MIN_WAIFU_PRICE, (long)(waifu.Price * (1 - MANAGERLESS_PRICE_DECAY)));
                    if (newPrice != waifu.Price)
                    {
                        await ctx.GetTable<WaifuInfo>()
                            .Where(x => x.Id == waifu.Id)
                            .Set(x => x.Price, newPrice)
                            .UpdateAsync();
                    }

                    Log.Information("Waifu {UserId} has no manager. Price decayed {OldPrice} -> {NewPrice}",
                        waifu.UserId, waifu.Price, newPrice);

                    continue;
                }

                await ProcessWaifuCycleInternalAsync(ctx, waifu, cycleNumber);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing cycle {Cycle} for waifu {UserId}", cycleNumber, waifu.UserId);
            }
        }

        // Log pending fan changes for next cycle
        var cycleStartTime = GetCycleStartTime(cycleNumber);
        foreach (var waifu in waifus)
        {
            var newFans = await ctx.GetTable<WaifuFan>()
                .CountAsyncLinqToDB(x => x.WaifuUserId == waifu.UserId
                                        && x.LeftAt == null
                                        && x.DelegatedAt >= cycleStartTime);

            var leavingFans = await ctx.GetTable<WaifuFan>()
                .CountAsyncLinqToDB(x => x.WaifuUserId == waifu.UserId
                                        && x.LeftAt != null
                                        && x.LeftAt >= cycleStartTime);

            if (newFans > 0 || leavingFans > 0)
            {
                Log.Information("Waifu {UserId} fan changes for next cycle: +{Joined} joining, -{Left} leaving",
                    waifu.UserId, newFans, leavingFans);
            }
        }

        // Cleanup soft-deleted fan rows that are no longer needed
        var cleaned = await ctx.GetTable<WaifuFan>()
            .Where(x => x.LeftAt != null && x.LeftAt < cycleStartTime)
            .DeleteAsync();

        if (cleaned > 0)
            Log.Information("Cleaned up {Count} old soft-deleted fan rows", cleaned);

        Log.Information("Waifu cycle #{Cycle} processing complete", cycleNumber);
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

        var effectiveRate = Math.Min(BASE_RETURN_RATE, MAX_RETURN_RATE);
        var cycleRate = effectiveRate / CYCLES_PER_YEAR;
        var statMultiplier = (waifu.Mood + waifu.Food) / 2000.0;
        var cappedBacked = Math.Min(totalBacked, waifu.ReturnsCap);

        var returns = (long)(cappedBacked * cycleRate * statMultiplier);
        if (returns <= 0)
        {
            Log.Information("Waifu {UserId} cycle #{Cycle}: returns=0 (mood={Mood}, food={Food}, backed={Backed})",
                waifu.UserId, cycleNumber, waifu.Mood, waifu.Food, totalBacked);
            return;
        }

        var waifuCut = (long)(returns * waifu.WaifuFeePercent / 100.0);
        var managerCut = (long)(waifuCut * MANAGER_CUT_PERCENT);
        var waifuNet = waifuCut - managerCut;
        var fanPool = returns - waifuCut;

        Log.Information(
            "Waifu {UserId} cycle #{Cycle}: backed={TotalBacked} capped={Capped} rate={Rate:P2} stats={Stats:P0} returns={Returns} | waifu={WaifuNet} manager={ManagerCut} fanPool={FanPool} fee={Fee}%",
            waifu.UserId, cycleNumber, totalBacked, cappedBacked, effectiveRate, statMultiplier,
            returns, waifuNet, managerCut, fanPool, waifu.WaifuFeePercent);

        // Pay waifu
        if (waifuNet > 0)
            await cs.AddAsync(waifu.UserId, waifuNet, new("waifu", "cycle-earnings"));

        // Pay manager (their cut + proportional fan share)
        var managerFanShare = 0L;
        var managerSnapshot = snapshots.FirstOrDefault(x => x.UserId == managerId);
        if (managerSnapshot.Balance > 0 && totalBacked > 0)
            managerFanShare = (long)(fanPool * ((double)managerSnapshot.Balance / totalBacked));

        var totalManagerPay = managerCut + managerFanShare;
        if (totalManagerPay > 0)
            await cs.AddAsync(managerId, totalManagerPay, new("waifu", "manager-earnings"));

        Log.Information("  Manager {ManagerId}: cut={ManagerCut} + fanShare={FanShare} = {Total}",
            managerId, managerCut, managerFanShare, totalManagerPay);

        // Pay fans proportionally (excluding manager's fan share already paid)
        foreach (var (fanId, balance) in snapshots)
        {
            if (fanId == managerId)
                continue;

            var share = (long)(fanPool * ((double)balance / totalBacked));
            if (share > 0)
                await cs.AddAsync(fanId, share, new("waifu", "fan-earnings"));

            Log.Information("  Fan {FanId}: balance={Balance} share={Share}",
                fanId, balance, share);
        }

        await ctx.GetTable<WaifuCycle>()
            .InsertAsync(() => new WaifuCycle
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
    public double RealizedReturnRate { get; init; }
    public long TotalReturns { get; init; }
    public long AvgBacked { get; init; }
    public int CyclesActive { get; init; }
}

#endregion
