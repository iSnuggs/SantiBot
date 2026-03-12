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

    private const double CYCLE_HOURS = 5.0 / 60.0;
    private const double BASE_RETURN_RATE = 0.17 * (84.0 / CYCLE_HOURS);
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
        Assert.That(_svc.GetTimeUntilPayout().TotalMinutes, Is.EqualTo(5.0).Within(0.01));

        _time.SetUtcNow(new(Epoch.AddMinutes(2.5), TimeSpan.Zero));
        Assert.That(_svc.GetCurrentCycle(), Is.EqualTo(0));
        Assert.That(_svc.GetCycleProgressFraction(), Is.EqualTo(0.5).Within(0.01));

        _time.SetUtcNow(new(Epoch.AddMinutes(5), TimeSpan.Zero));
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
        var expectedReturns = (long)(totalBacked * cycleRate * 1.0);
        var waifuCut = (long)(expectedReturns * 5 / 100.0);
        var managerCut = (long)(waifuCut * 0.15);
        var waifuNet = waifuCut - managerCut;
        var fanPool = expectedReturns - waifuCut;

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
        var expReturns = (long)(1_000_000_000 * cycleRate * 1.0);
        var expWaifuCut = (long)(expReturns * 5 / 100.0);
        var expManagerCut = (long)(expWaifuCut * 0.15);
        var expWaifuNet = expWaifuCut - expManagerCut;
        var expFanPool = expReturns - expWaifuCut;

        await using (var ctx = _db.GetDbContext())
        {
            var cycle = await ctx.GetTable<WaifuCycle>()
                .FirstAsyncLinqToDB(x => x.WaifuUserId == 1001 && x.CycleNumber == cycleNumber);
            Assert.That(cycle.TotalReturns, Is.EqualTo(expReturns));
            Assert.That(cycle.WaifuEarnings, Is.EqualTo(expWaifuNet));
            Assert.That(cycle.ManagerEarnings, Is.EqualTo(expManagerCut));
            Assert.That(cycle.FanPool, Is.EqualTo(expFanPool));
        }

        var expManagerFanShare = (long)(expFanPool * 0.5);

        // Cycle processing now writes to pending payouts, not cs.AddAsync
        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());

        await using (var ctx = _db.GetDbContext())
        {
            var waifuPending = await ctx.GetTable<WaifuPendingPayout>()
                .FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That((long)Math.Floor(waifuPending.Amount), Is.EqualTo(expWaifuNet));

            var managerPending = await ctx.GetTable<WaifuPendingPayout>()
                .FirstAsyncLinqToDB(x => x.UserId == 2001);
            Assert.That((long)Math.Floor(managerPending.Amount), Is.EqualTo(expManagerCut + expManagerFanShare));

            var fanAPending = await ctx.GetTable<WaifuPendingPayout>()
                .FirstAsyncLinqToDB(x => x.UserId == 3001);
            Assert.That((long)Math.Floor(fanAPending.Amount), Is.EqualTo((long)(expFanPool * 0.3)));

            var fanBPending = await ctx.GetTable<WaifuPendingPayout>()
                .FirstAsyncLinqToDB(x => x.UserId == 4001);
            Assert.That((long)Math.Floor(fanBPending.Amount), Is.EqualTo((long)(expFanPool * 0.2)));
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
        }
    }
}
