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
using OneOf;
using OneOf.Types;

namespace NadekoBot.Tests.Waifu;

[TestFixture]
public class WaifuQueryTests
{
    private WaifuService _svc = null!;
    private ICurrencyService _cs = null!;
    private IBotCache _cache = null!;
    private TestDbService _db = null!;

    private const double CYCLE_HOURS = 84.0;
    private const double BASE_RETURN_RATE = 0.17;
    private static readonly double CYCLES_PER_YEAR = 365.25 * 24 / CYCLE_HOURS;

    [SetUp]
    public void Setup()
    {
        _db = new TestDbService();
        _cs = Substitute.For<ICurrencyService>();
        _cache = Substitute.For<IBotCache>();
        var time = new FakeTimeProvider(new(2025, 1, 7, 0, 0, 0, TimeSpan.Zero));
        var client = Substitute.For<DiscordSocketClient>();
        _svc = new WaifuService(_db, _cache, _cs, client, WaifuTestHelper.CreateConfigService(), null!, time);
        _cs.RemoveAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>()).Returns(true);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetWaifuInfo_OptedIn_ReturnsFullDto()
    {
        await using (var ctx = _db.GetDbContext())
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 800, food: 600, fee: 3, price: 5000,
                managerId: 2001, returnsCap: 500_000, isHubby: true);

        var result = await _svc.GetWaifuInfoAsync(1001);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Mood, Is.EqualTo(800));
        Assert.That(result.Food, Is.EqualTo(600));
        Assert.That(result.WaifuFeePercent, Is.EqualTo(3));
        Assert.That(result.Price, Is.EqualTo(5000));
        Assert.That(result.ManagerId, Is.EqualTo(2001));
        Assert.That(result.IsHubby, Is.True);
    }

    [Test]
    public async Task GetWaifuInfo_NonExistent_ReturnsNull()
    {
        Assert.That(await _svc.GetWaifuInfoAsync(9999), Is.Null);
    }

    [Test]
    public async Task GetBacking_ActiveFan_ReturnsWaifuId()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
        }

        Assert.That(await _svc.GetBackingAsync(2001), Is.EqualTo(1001));
        Assert.That(await _svc.GetBackingAsync(9999), Is.Null);
    }

    [Test]
    public async Task IsWaifu_IsFan_IsManager_ReturnCorrectBooleans()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, managerId: 5001);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
        }

        Assert.That(await _svc.IsWaifuAsync(1001), Is.True);
        Assert.That(await _svc.IsWaifuAsync(9999), Is.False);
        Assert.That(await _svc.IsFanAsync(2001), Is.True);
        Assert.That(await _svc.IsFanAsync(9999), Is.False);
        Assert.That(await _svc.IsManagerAsync(5001), Is.True);
        Assert.That(await _svc.IsManagerAsync(9999), Is.False);
    }

    [Test]
    public async Task GetManagerExitInfo_ReturnsCorrectBreakdown()
    {
        await using (var ctx = _db.GetDbContext())
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 10_000, managerId: 5001);

        var result = await _svc.GetManagerExitInfoAsync(5001, 1001);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Refund, Is.EqualTo(5000));
        Assert.That(result.WaifuCut, Is.EqualTo(1000));
        Assert.That(result.FanDistribution, Is.EqualTo(3500));
        Assert.That(result.Burned, Is.EqualTo(500));

        Assert.That(await _svc.GetManagerExitInfoAsync(9999, 1001), Is.Null);
    }

    [Test]
    public async Task GetFans_WithCycleHistory_ReturnsEarningsAndPending()
    {
        var currentCycle = _svc.GetCurrentCycle();
        var lastCycle = currentCycle - 1;

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, managerId: 5001);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
            await WaifuTestHelper.CreateFan(ctx, 3001, 1001);

            // Only fan 2001 in current snapshot (3001 is pending)
            await WaifuTestHelper.CreateCycleSnapshot(ctx, currentCycle, 1001, 2001, 5000);

            // Last cycle data
            await WaifuTestHelper.CreateCycleSnapshot(ctx, lastCycle, 1001, 2001, 5000);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, lastCycle, 1001, 3001, 3000);
            await WaifuTestHelper.CreateCycleRecord(ctx, 1001, lastCycle, 5001,
                totalBacked: 8000, totalReturns: 100, waifuEarnings: 4, managerEarnings: 1, fanPool: 95);
        }

        var result = await _svc.GetFansAsync(1001);
        Assert.That(result, Has.Count.EqualTo(2));

        var fan2001 = result.First(x => x.UserId == 2001);
        Assert.That(fan2001.LastCycleEarnings, Is.EqualTo((long)(95 * (5000.0 / 8000))));
        Assert.That(fan2001.IsPending, Is.False);

        var fan3001 = result.First(x => x.UserId == 3001);
        Assert.That(fan3001.IsPending, Is.True);
    }

    [Test]
    public async Task GetLeaderboard_RankedByPrice()
    {
        var currentCycle = _svc.GetCurrentCycle();

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 5000);
            await WaifuTestHelper.CreateCycleRecord(ctx, 1001, currentCycle - 1, 5001,
                totalBacked: 10000, totalReturns: 500, waifuEarnings: 20, managerEarnings: 5, fanPool: 475);

            await WaifuTestHelper.CreateWaifu(ctx, 2001, price: 10000);
            await WaifuTestHelper.CreateCycleRecord(ctx, 2001, currentCycle - 1, 6001,
                totalBacked: 20000, totalReturns: 200, waifuEarnings: 8, managerEarnings: 2, fanPool: 190);
        }

        var result = await _svc.GetLeaderboardAsync(WaifuLbOrder.Price);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].UserId, Is.EqualTo(2001), "Higher price should be first");
        Assert.That(result[0].Price, Is.EqualTo(10000));
        Assert.That(result[1].Price, Is.EqualTo(5000));
    }

    [Test]
    public async Task GetProjectedPayout_WithSnapshotData_ReturnsCorrectProjection()
    {
        var currentCycle = _svc.GetCurrentCycle();

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, mood: 1000, food: 1000, fee: 5, managerId: 5001,
                returnsCap: 1_000_000_000);
            await WaifuTestHelper.CreateCycleSnapshot(ctx, currentCycle, 1001, 2001, 100_000_000);
        }

        var result = await _svc.GetProjectedPayoutAsync(1001);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalReturns, Is.GreaterThan(0));

        var cycleRate = BASE_RETURN_RATE / CYCLES_PER_YEAR;
        var expectedReturns = (long)(100_000_000 * cycleRate * 1.0);
        Assert.That(result.TotalReturns, Is.EqualTo(expectedReturns));
    }

    [Test]
    public async Task GetRemainingActions_ReturnsCorrectCount()
    {
        _cache.GetAsync(Arg.Any<TypedKey<int>>())
            .Returns(new System.Threading.Tasks.ValueTask<OneOf<int, None>>(new None()));
        Assert.That(await _svc.GetRemainingActionsAsync(1001), Is.EqualTo(2));

        _cache.GetAsync(Arg.Any<TypedKey<int>>())
            .Returns(new System.Threading.Tasks.ValueTask<OneOf<int, None>>((OneOf<int, None>)1));
        Assert.That(await _svc.GetRemainingActionsAsync(1001), Is.EqualTo(1));
    }
}
