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
using NadekoBot.Modules.Waifus.WaifusHubbies;
using NadekoBot.Modules.Waifus.WaifusHubbies.Db;
using NadekoBot.Services;
using NadekoBot.Services.Currency;
using NSubstitute;
using NUnit.Framework;

namespace NadekoBot.Tests.Waifu;

[TestFixture]
public class WnHIntegrationTests
{
    private WnHService _svc = null!;
    private ICurrencyService _cs = null!;
    private TestDbService _db = null!;

    [SetUp]
    public void Setup()
    {
        _db = new TestDbService();
        _cs = Substitute.For<ICurrencyService>();
        var cache = Substitute.For<IBotCache>();
        var time = new FakeTimeProvider(new(2025, 1, 7, 0, 0, 0, TimeSpan.Zero));
        var client = Substitute.For<DiscordSocketClient>();
        _svc = new WnHService(_db, cache, _cs, client, null!, time);
        _cs.RemoveAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>()).Returns(true);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task FullLifecycle_OptInFanManagerCyclePayout()
    {
        // Opt in
        var optIn = await _svc.OptInAsync(1001);
        Assert.That(optIn.IsT2, Is.True);

        // Fans join
        await _svc.BecomeFanAsync(2001, 1001);
        await _svc.BecomeFanAsync(3001, 1001);

        // Buy manager
        var buy = await _svc.BuyManagerAsync(3001, 1001);
        Assert.That(buy.IsT5, Is.True);

        // Boost stats and set bank balances for meaningful cycle payouts
        await using (var ctx = _db.GetDbContext())
        {
            await ctx.GetTable<WaifuInfo>().Where(x => x.UserId == 1001)
                .Set(x => x.ReturnsCap, 500_000_000L)
                .Set(x => x.Mood, 1000)
                .Set(x => x.Food, 1000)
                .UpdateAsync();
            await WnHTestHelper.SetBankBalance(ctx, 2001, 60_000_000);
            await WnHTestHelper.SetBankBalance(ctx, 3001, 40_000_000);
        }

        // Snapshot and process cycle
        var cycleNumber = _svc.GetCurrentCycle();
        await _svc.SnapshotCycleInternalAsync(cycleNumber);
        _cs.ClearReceivedCalls();

        await using (var ctx = _db.GetDbContext())
            await _svc.ProcessCycleInternalAsync(ctx, cycleNumber);

        // Verify cycle record created with non-zero returns
        await using (var ctx = _db.GetDbContext())
        {
            var cycle = await ctx.GetTable<WaifuCycle>()
                .FirstOrDefaultAsyncLinqToDB(x => x.WaifuUserId == 1001 && x.CycleNumber == cycleNumber);
            Assert.That(cycle, Is.Not.Null);
            Assert.That(cycle!.TotalReturns, Is.GreaterThan(0));
            Assert.That(cycle.TotalBacked, Is.EqualTo(100_000_000));

            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.TotalProduced, Is.EqualTo(cycle.TotalReturns));
        }
    }

    [Test]
    public async Task ManagerExit_ThenNewManagerBuysAtReducedPrice()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WnHTestHelper.CreateWaifu(ctx, 1001, price: 10_000, managerId: 3001);
            await WnHTestHelper.CreateFan(ctx, 3001, 1001);
            await ctx.GetTable<WaifuFan>()
                .InsertAsync(() => new WaifuFan { UserId = 2001, WaifuUserId = 1001, DelegatedAt = DateTime.UtcNow });
            await WnHTestHelper.SetBankBalance(ctx, 2001, 5_000);
        }

        // Manager exits -> price drops to 5000
        await _svc.StopBeingFanAsync(3001);
        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.Price, Is.EqualTo(5000));
            Assert.That(wi.ManagerUserId, Is.Null);
        }

        // Fan B buys at reduced price: ceil(5000*1.15)=5750
        _cs.ClearReceivedCalls();
        var buy = await _svc.BuyManagerAsync(2001, 1001);
        Assert.That(buy.IsT5, Is.True);
        Assert.That(buy.AsT5.Value.PricePaid, Is.EqualTo(5750));

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.ManagerUserId, Is.EqualTo(2001));
        }
    }
}
