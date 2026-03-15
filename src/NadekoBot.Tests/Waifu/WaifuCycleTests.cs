#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Nadeko.Common;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Waifus.Waifu;
using NadekoBot.Modules.Waifus.Waifu.Db;
using NadekoBot.Services;
using NadekoBot.Services.Currency;
using NSubstitute;
using NUnit.Framework;

namespace NadekoBot.Tests.Waifu;

[TestFixture]
public class WaifuCycleTests
{
    private WaifuService _svc = null!;
    private ICurrencyService _cs = null!;
    private TestDbService _db = null!;
    private FakeTimeProvider _time = null!;

    private const double CYCLE_HOURS = 84.0;
    private const double BASE_RETURN_RATE = 0.17;
    private static readonly double CYCLES_PER_YEAR = 365.25 * 24 / CYCLE_HOURS;
    private static readonly DateTime Epoch = new(2025, 1, 7, 0, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void Setup()
    {
        _db = new TestDbService();
        _cs = Substitute.For<ICurrencyService>();
        var cache = Substitute.For<IBotCache>();
        _time = new FakeTimeProvider(new(Epoch, TimeSpan.Zero));
        var client = Substitute.For<DiscordSocketClient>();
        _svc = new WaifuService(_db, cache, _cs, client, WaifuTestHelper.CreateConfigService(), null!, _time);
        _cs.RemoveAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>()).Returns(true);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void CycleHelpers_CorrectCycleNumberAndProgress()
    {
        Assert.That(_svc.GetCurrentCycle(), Is.EqualTo(0));
        Assert.That(_svc.GetCycleStartTime(0), Is.EqualTo(Epoch));
        Assert.That(_svc.GetCycleProgressFraction(), Is.EqualTo(0.0).Within(0.001));
        Assert.That(_svc.GetTimeUntilPayout().TotalHours, Is.EqualTo(84.0).Within(0.01));

        _time.SetUtcNow(new(Epoch.AddHours(42), TimeSpan.Zero));
        Assert.That(_svc.GetCurrentCycle(), Is.EqualTo(0));
        Assert.That(_svc.GetCycleProgressFraction(), Is.EqualTo(0.5).Within(0.01));

        _time.SetUtcNow(new(Epoch.AddHours(84), TimeSpan.Zero));
        Assert.That(_svc.GetCurrentCycle(), Is.EqualTo(1));
    }

    [Test]
    public async Task Snapshot_CapturesActiveFanBalances()
    {
        var cycleNumber = _svc.GetCurrentCycle();

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await WaifuTestHelper.SetBankBalance(ctx, 2001, 5000);
        }

        await _svc.SnapshotCycleInternalAsync(cycleNumber);

        await using (var ctx = _db.GetDbContext())
        {
            var snapshots = await ctx.GetTable<WaifuCycleSnapshot>()
                .Where(x => x.WaifuUserId == 1001 && x.CycleNumber == cycleNumber)
                .ToListAsyncLinqToDB();

            Assert.That(snapshots, Has.Count.EqualTo(1));
            Assert.That(snapshots[0].UserId, Is.EqualTo(2001));
            Assert.That(snapshots[0].SnapshotBalance, Is.EqualTo(5000));
        }
    }

    [Test]
    public async Task CycleProcessing_PayoutFormula_CorrectDistribution()
    {
        long totalBacked = 100_000_000;
        long returnsCap = 500_000_000;
        var cycleRate = BASE_RETURN_RATE / CYCLES_PER_YEAR;
        var returnsD = (decimal)(totalBacked * cycleRate * 1.0);
        var waifuCutD = returnsD * 5 / 100m;
        var managerCutD = waifuCutD * 0.15m;
        var waifuNetD = waifuCutD - managerCutD;
        var fanPoolD = returnsD - waifuCutD;
        var expectedReturns = (long)returnsD;
        var waifuNet = (long)waifuNetD;
        var managerCut = (long)managerCutD;
        var fanPool = (long)fanPoolD;

        var cycleNumber = _svc.GetCurrentCycle();

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 1000, food: 1000, fee: 5,
                managerId: 2001, returnsCap: returnsCap);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, cycleNumber, 1001, 2001, totalBacked);
        }

        await using (var ctx = _db.GetDbContext())
        {
            var waifu = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            await _svc.ProcessWaifuCycleInternalAsync(ctx, waifu, cycleNumber);
        }

        await using (var ctx = _db.GetDbContext())
        {
            var cycle = await ctx.GetTable<WaifuCycle>()
                .FirstAsyncLinqToDB(x => x.WaifuUserId == 1001 && x.CycleNumber == cycleNumber);

            Assert.That(cycle.TotalReturns, Is.EqualTo(expectedReturns));
            Assert.That(cycle.WaifuEarnings, Is.EqualTo(waifuNet));
            Assert.That(cycle.ManagerEarnings, Is.EqualTo(managerCut));
            Assert.That(cycle.FanPool, Is.EqualTo(fanPool));

            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.TotalProduced, Is.EqualTo(expectedReturns));
        }
    }

    [Test]
    public async Task CycleProcessing_ZeroStats_ProducesNoReturns()
    {
        var cycleNumber = _svc.GetCurrentCycle();

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 0, food: 0, managerId: 2001, returnsCap: 500_000_000);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, cycleNumber, 1001, 2001, 100_000_000);
        }

        await using (var ctx = _db.GetDbContext())
        {
            var waifu = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            await _svc.ProcessWaifuCycleInternalAsync(ctx, waifu, cycleNumber);
        }

        await using (var ctx = _db.GetDbContext())
        {
            var cycle = await ctx.GetTable<WaifuCycle>()
                .FirstOrDefaultAsyncLinqToDB(x => x.WaifuUserId == 1001 && x.CycleNumber == cycleNumber);
            Assert.That(cycle, Is.Null);
        }
        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());

        await using (var ctx = _db.GetDbContext())
        {
            var payouts = await ctx.GetTable<WaifuPendingPayout>().ToListAsyncLinqToDB();
            Assert.That(payouts, Is.Empty);
        }
    }

    [Test]
    public async Task Decay_DecrementsStatsWhenOverdue_FlooredAtZero()
    {
        _time.SetUtcNow(new(2025, 1, 7, 3, 0, 0, TimeSpan.Zero));

        await using (var ctx = _db.GetDbContext())
        {
            // Normal waifu: stats should decrement
            await ctx.GetTable<WaifuInfo>().InsertAsync(() => new WaifuInfo
            {
                UserId = 1001, Mood = 500, Food = 500, WaifuFeePercent = 5, Price = 1000,
                ReturnsCap = 1_000_000, IsHubby = false,
                LastDecayTime = new DateTime(2025, 1, 7, 0, 0, 0, DateTimeKind.Utc), TotalProduced = 0
            });
            // Zero-stat waifu: should floor at 0
            await ctx.GetTable<WaifuInfo>().InsertAsync(() => new WaifuInfo
            {
                UserId = 1002, Mood = 0, Food = 0, WaifuFeePercent = 5, Price = 1000,
                ReturnsCap = 1_000_000, IsHubby = false,
                LastDecayTime = new DateTime(2025, 1, 7, 0, 0, 0, DateTimeKind.Utc), TotalProduced = 0
            });
        }

        await _svc.DecayInternalAsync();

        await using (var ctx = _db.GetDbContext())
        {
            var wi1 = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi1.Mood, Is.EqualTo(499));
            Assert.That(wi1.Food, Is.EqualTo(499));

            var wi2 = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1002);
            Assert.That(wi2.Mood, Is.EqualTo(0));
            Assert.That(wi2.Food, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task CycleProcessing_Idempotent_NoDuplicates()
    {
        var cycleNumber = _svc.GetCurrentCycle();

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 1000, food: 1000, managerId: 2001, returnsCap: 500_000_000);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, cycleNumber, 1001, 2001, 50_000_000);
        }

        await using (var ctx = _db.GetDbContext())
            await _svc.ProcessCycleInternalAsync(ctx, cycleNumber);
        _cs.ClearReceivedCalls();
        await using (var ctx = _db.GetDbContext())
            await _svc.ProcessCycleInternalAsync(ctx, cycleNumber);

        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());
    }

    [Test]
    public async Task CycleProcessing_MultiBackerPayout_CorrectDistribution()
    {
        // Manager=500M (50%), FanA=300M (30%), FanB=200M (20%), total=1B
        // Full stats, fee=5%, returnsCap=5B
        var cycleNumber = _svc.GetCurrentCycle();
        long manager = 500_000_000, fanA = 300_000_000, fanB = 200_000_000;

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 1000, food: 1000, fee: 5,
                managerId: 2001, returnsCap: 5_000_000_000);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await ctx.GetTable<WaifuFan>()
                .InsertAsync(() => new WaifuFan { UserId = 3001, WaifuUserId = 1001, DelegatedAt = DateTime.UtcNow });
            await ctx.GetTable<WaifuFan>()
                .InsertAsync(() => new WaifuFan { UserId = 4001, WaifuUserId = 1001, DelegatedAt = DateTime.UtcNow });
            await WaifuTestHelper.CreateCycleSnapshot(ctx, cycleNumber, 1001, 2001, manager);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, cycleNumber, 1001, 3001, fanA);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, cycleNumber, 1001, 4001, fanB);
        }

        _cs.ClearReceivedCalls();
        await using (var ctx = _db.GetDbContext())
        {
            var waifu = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            await _svc.ProcessWaifuCycleInternalAsync(ctx, waifu, cycleNumber);
        }

        // total=1B, cap=5B, fee=5%, full stats
        var cycleRate = BASE_RETURN_RATE / CYCLES_PER_YEAR;
        var expReturnsD = (decimal)(1_000_000_000 * cycleRate * 1.0);
        var expWaifuCutD = expReturnsD * 5 / 100m;
        var expManagerCutD = expWaifuCutD * 0.15m;
        var expWaifuNetD = expWaifuCutD - expManagerCutD;
        var expFanPoolD = expReturnsD - expWaifuCutD;
        var expReturns = (long)expReturnsD;
        var expWaifuNet = (long)expWaifuNetD;
        var expManagerCut = (long)expManagerCutD;
        var expFanPool = (long)expFanPoolD;

        await using (var ctx = _db.GetDbContext())
        {
            var cycle = await ctx.GetTable<WaifuCycle>()
                .FirstAsyncLinqToDB(x => x.WaifuUserId == 1001 && x.CycleNumber == cycleNumber);
            Assert.That(cycle.TotalReturns, Is.EqualTo(expReturns));
            Assert.That(cycle.WaifuEarnings, Is.EqualTo(expWaifuNet));
            Assert.That(cycle.ManagerEarnings, Is.EqualTo(expManagerCut));
            Assert.That(cycle.FanPool, Is.EqualTo(expFanPool));
        }

        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());

        await using (var ctx = _db.GetDbContext())
        {
            var waifuPending = await ctx.GetTable<WaifuPendingPayout>()
                .FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That((long)Math.Floor(waifuPending.Amount), Is.EqualTo(expWaifuNet));

            var managerPending = await ctx.GetTable<WaifuPendingPayout>()
                .FirstAsyncLinqToDB(x => x.UserId == 2001);
            Assert.That((long)Math.Floor(managerPending.Amount), Is.EqualTo((long)(expManagerCutD + expFanPoolD * 0.5m)));

            var fanAPending = await ctx.GetTable<WaifuPendingPayout>()
                .FirstAsyncLinqToDB(x => x.UserId == 3001);
            Assert.That((long)Math.Floor(fanAPending.Amount), Is.EqualTo((long)(expFanPoolD * 0.3m)));

            var fanBPending = await ctx.GetTable<WaifuPendingPayout>()
                .FirstAsyncLinqToDB(x => x.UserId == 4001);
            Assert.That((long)Math.Floor(fanBPending.Amount), Is.EqualTo((long)(expFanPoolD * 0.2m)));
        }
    }

    [Test]
    public async Task CatchUp_ProcessesMissedCycle()
    {
        // Simulate: cycle 0 was snapshotted but never processed, now we're in cycle 1
        _time.SetUtcNow(new(Epoch.AddHours(CYCLE_HOURS), TimeSpan.Zero));

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 1000, food: 1000, fee: 5,
                managerId: 2001, returnsCap: 500_000_000);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, 0, 1001, 2001, 100_000_000);
        }

        Assert.That(_svc.GetCurrentCycle(), Is.EqualTo(1));

        await _svc.CatchUpMissedCyclesInternalAsync();

        await using (var ctx = _db.GetDbContext())
        {
            var cycle = await ctx.GetTable<WaifuCycle>()
                .FirstOrDefaultAsyncLinqToDB(x => x.WaifuUserId == 1001 && x.CycleNumber == 0);
            Assert.That(cycle, Is.Not.Null);
            Assert.That(cycle!.TotalReturns, Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task CatchUp_SkipsAlreadyProcessedCycle()
    {
        _time.SetUtcNow(new(Epoch.AddHours(CYCLE_HOURS), TimeSpan.Zero));

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 1000, food: 1000, fee: 5,
                managerId: 2001, returnsCap: 500_000_000);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, 0, 1001, 2001, 100_000_000);
            await WaifuTestHelper.CreateCycleRecord(ctx, 1001, 0, 2001, 100_000_000, 1000, 42, 6, 952);
        }

        _cs.ClearReceivedCalls();
        await _svc.CatchUpMissedCyclesInternalAsync();

        // No new payouts should have been added
        await using (var ctx = _db.GetDbContext())
        {
            var payouts = await ctx.GetTable<WaifuPendingPayout>().ToListAsyncLinqToDB();
            Assert.That(payouts, Is.Empty);
        }
    }

    [Test]
    public async Task CycleProcessing_ManagerlessWaifu_PriceDecays()
    {
        var cycleNumber = _svc.GetCurrentCycle();

        await using (var ctx = _db.GetDbContext())
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 5000); // no manager

        await using (var ctx = _db.GetDbContext())
            await _svc.ProcessCycleInternalAsync(ctx, cycleNumber);

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.Price, Is.EqualTo(4500)); // 5000 * 0.90, floored at 1000

            var cycle = await ctx.GetTable<WaifuCycle>()
                .FirstOrDefaultAsyncLinqToDB(x => x.WaifuUserId == 1001 && x.CycleNumber == cycleNumber);
            Assert.That(cycle, Is.Not.Null);
            Assert.That(cycle!.TotalReturns, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task CatchUp_ManagerlessWaifu_NoDoubleDecay()
    {
        _time.SetUtcNow(new(Epoch.AddHours(CYCLE_HOURS), TimeSpan.Zero));

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 5000); // no manager
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, 0, 1001, 2001, 100_000);
        }

        await _svc.CatchUpMissedCyclesInternalAsync();

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.Price, Is.EqualTo(4500)); // decayed once
        }

        // Second catch-up should not decay again
        await _svc.CatchUpMissedCyclesInternalAsync();

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.Price, Is.EqualTo(4500)); // still 4500, not 4050
        }
    }

    [Test]
    public async Task CleanupOldCycleData_DeletesOldRecords_KeepsRecent()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 1000, food: 1000, managerId: 2001, returnsCap: 500_000_000);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);

            // Old data (cycle 0 and 5)
            await WaifuTestHelper.CreateCycleSnapshot(ctx, 0, 1001, 2001, 100_000);
            await WaifuTestHelper.CreateCycleRecord(ctx, 1001, 0, 2001, 100_000, 50, 2, 1, 47);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, 4, 1001, 2001, 200_000);
            await WaifuTestHelper.CreateCycleRecord(ctx, 1001, 4, 2001, 200_000, 100, 4, 2, 94);

            // Recent data (cycle 6 and 15)
            await WaifuTestHelper.CreateCycleSnapshot(ctx, 6, 1001, 2001, 300_000);
            await WaifuTestHelper.CreateCycleRecord(ctx, 1001, 6, 2001, 300_000, 150, 6, 3, 141);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, 15, 1001, 2001, 400_000);
            await WaifuTestHelper.CreateCycleRecord(ctx, 1001, 15, 2001, 400_000, 200, 8, 4, 188);
        }

        // Cleanup with currentCycle=15 -> cutoff=5 -> deletes cycles < 5
        await using (var ctx = _db.GetDbContext())
            await _svc.CleanupOldCycleDataInternalAsync(ctx, 15);

        await using (var ctx = _db.GetDbContext())
        {
            var snapshots = await ctx.GetTable<WaifuCycleSnapshot>().ToListAsyncLinqToDB();
            var cycles = await ctx.GetTable<WaifuCycle>().ToListAsyncLinqToDB();

            // Cycle 0 deleted, cycles 4, 6, 15 remain
            Assert.That(snapshots.Any(x => x.CycleNumber == 0), Is.False);
            Assert.That(snapshots.Any(x => x.CycleNumber == 4), Is.False);
            Assert.That(snapshots.Any(x => x.CycleNumber == 6), Is.True);
            Assert.That(snapshots.Any(x => x.CycleNumber == 15), Is.True);

            Assert.That(cycles.Any(x => x.CycleNumber == 0), Is.False);
            Assert.That(cycles.Any(x => x.CycleNumber == 4), Is.False);
            Assert.That(cycles.Any(x => x.CycleNumber == 6), Is.True);
            Assert.That(cycles.Any(x => x.CycleNumber == 15), Is.True);
        }
    }

    [Test]
    public async Task CleanupOldCycleData_NothingToDelete_NoError()
    {
        await using (var ctx = _db.GetDbContext())
            await _svc.CleanupOldCycleDataInternalAsync(ctx, 5);

        // No exception = success
        Assert.Pass();
    }
}
