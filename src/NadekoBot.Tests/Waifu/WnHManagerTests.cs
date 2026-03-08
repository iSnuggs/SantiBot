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
public class WnHManagerTests
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
    public async Task BuyManager_FirstManager_WaifuGetsFullPayout()
    {
        // Price=1000, required=ceil(1000*1.15)=1150
        // No old manager: waifu gets oldManagerPayout+waifuPayout = 1035+57 = 1092
        await using (var ctx = _db.GetDbContext())
        {
            await WnHTestHelper.CreateWaifu(ctx, 1001, price: 1000);
            await WnHTestHelper.CreateFan(ctx, 2001, 1001);
        }

        var result = await _svc.BuyManagerAsync(2001, 1001);
        Assert.That(result.IsT5, Is.True, "Expected Success<BuyManagerInfo>");

        var info = result.AsT5.Value;
        Assert.That(info.PricePaid, Is.EqualTo(1150));
        Assert.That(info.OldManagerId, Is.Null);
        Assert.That(info.WaifuPayout, Is.EqualTo(1092));
        Assert.That(info.Burned, Is.EqualTo(58));

        await _cs.Received(1).RemoveAsync(2001, 1150, Arg.Any<TxData?>());
        await _cs.Received(1).AddAsync(1001, 1092, Arg.Any<TxData?>());

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.ManagerUserId, Is.EqualTo(2001));
            Assert.That(wi.Price, Is.EqualTo(1150));
        }
    }

    [Test]
    public async Task BuyManager_ReplaceExistingManager_OldManagerPaidOut()
    {
        // Price=2000, required=ceil(2000*1.15)=2300
        // Old manager gets 90%=2070, waifu gets 5%=115
        await using (var ctx = _db.GetDbContext())
        {
            await WnHTestHelper.CreateWaifu(ctx, 1001, price: 2000, managerId: 3001);
            await WnHTestHelper.CreateFan(ctx, 3001, 1001);
            await WnHTestHelper.CreateFan(ctx, 2001, 1001);
        }

        var result = await _svc.BuyManagerAsync(2001, 1001);
        Assert.That(result.IsT5, Is.True, "Expected Success");

        var info = result.AsT5.Value;
        Assert.That(info.OldManagerPayout, Is.EqualTo(2070));
        Assert.That(info.WaifuPayout, Is.EqualTo(115));
        await _cs.Received(1).AddAsync(3001, 2070, Arg.Any<TxData?>());
    }

    [Test]
    public async Task BuyManager_InsufficientFunds_ReturnsError()
    {
        _cs.RemoveAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>()).Returns(false);

        await using (var ctx = _db.GetDbContext())
        {
            await WnHTestHelper.CreateWaifu(ctx, 1001, price: 1000);
            await WnHTestHelper.CreateFan(ctx, 2001, 1001);
        }

        var result = await _svc.BuyManagerAsync(2001, 1001);
        Assert.That(result.IsT2, Is.True, "Expected ErrInsufficientFunds");
        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());
    }

    [Test]
    public async Task ManagerExit_WithFans_CorrectDistribution()
    {
        // Price=10000: refund=5000, waifuCut=1000, fanDist=3500
        await using (var ctx = _db.GetDbContext())
        {
            await WnHTestHelper.CreateWaifu(ctx, 1001, price: 10000, managerId: 2001);
            await WnHTestHelper.CreateFan(ctx, 2001, 1001);
            await ctx.GetTable<WaifuFan>()
                .InsertAsync(() => new WaifuFan { UserId = 3001, WaifuUserId = 1001, DelegatedAt = DateTime.UtcNow });
            await WnHTestHelper.SetBankBalance(ctx, 3001, 1000);
        }

        var result = await _svc.StopBeingFanAsync(2001);
        Assert.That(result.IsT1, Is.True, "Expected Success");

        await _cs.Received(1).AddAsync(2001, 5000, Arg.Any<TxData?>());
        await _cs.Received(1).AddAsync(1001, 1000, Arg.Any<TxData?>());
        await _cs.Received(1).AddAsync(3001, 3500, Arg.Any<TxData?>());

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.ManagerUserId, Is.Null);
            Assert.That(wi.Price, Is.EqualTo(5000));
        }
    }

    [Test]
    public async Task ManagerExit_PriceBelowFloor_ClampsToMinimum()
    {
        // Price=1500, refund=750 -> newPrice=max(1000,750)=1000
        await using (var ctx = _db.GetDbContext())
        {
            await WnHTestHelper.CreateWaifu(ctx, 1001, price: 1500, managerId: 2001);
            await WnHTestHelper.CreateFan(ctx, 2001, 1001);
        }

        await _svc.StopBeingFanAsync(2001);

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.Price, Is.EqualTo(1000));
        }
        await _cs.Received(1).AddAsync(2001, 750, Arg.Any<TxData?>());
    }
}
