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
public class WaifuManagerTests
{
    private WaifuService _svc = null!;
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
        _svc = new WaifuService(_db, cache, _cs, client, WaifuTestHelper.CreateConfigService(), null!, time);
        _cs.RemoveAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>()).Returns(true);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task BuyManager_FirstManager_WaifuGetsFullPayout()
    {
        // Price=1000, bid=1150 (min required=ceil(1000*1.15)=1150)
        // No old manager: waifu gets oldManagerPayout+waifuPayout = 1035+57 = 1092
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 1000);
        }

        var result = await _svc.BuyManagerAsync(2001, 1001, 1150);
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
        // Price=2000, bid=2300 (min required=ceil(2000*1.15)=2300)
        // Old manager gets 90%=2070, waifu gets 5%=115
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 2000, managerId: 3001);
        }

        var result = await _svc.BuyManagerAsync(2001, 1001, 2300);
        Assert.That(result.IsT5, Is.True, "Expected Success");

        var info = result.AsT5.Value;
        Assert.That(info.OldManagerPayout, Is.EqualTo(2070));
        Assert.That(info.WaifuPayout, Is.EqualTo(115));
        await _cs.Received(1).AddAsync(3001, 2070, Arg.Any<TxData?>());
    }

    [Test]
    public async Task BuyManager_BidTooLow_ReturnsError()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 1000);
        }

        // Min required = 1150, bid 1000
        var result = await _svc.BuyManagerAsync(2001, 1001, 1000);
        Assert.That(result.IsT3, Is.True, "Expected ErrPriceTooLow");
        await _cs.DidNotReceive().RemoveAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());
        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());
    }

    [Test]
    public async Task BuyManager_InsufficientFunds_ReturnsError()
    {
        _cs.RemoveAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>()).Returns(false);

        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 1000);
        }

        var result = await _svc.BuyManagerAsync(2001, 1001, 1150);
        Assert.That(result.IsT1, Is.True, "Expected ErrInsufficientFunds");
        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());
    }

    [Test]
    public async Task ResignManager_ClearsManagerAndDropsPrice()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 10000, managerId: 2001);
        }

        var result = await _svc.ResignManagerAsync(2001, 1001);
        Assert.That(result.IsT1, Is.True, "Expected Success");

        // No money should be created or moved
        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.ManagerUserId, Is.Null);
            Assert.That(wi.Price, Is.EqualTo(1000));

            // No pending payout should be created
            var payout = await ctx.GetTable<WaifuPendingPayout>()
                .FirstOrDefaultAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(payout, Is.Null);
        }
    }

    [Test]
    public async Task ResignManager_NotManager_ReturnsError()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 1000, managerId: 3001);
        }

        var result = await _svc.ResignManagerAsync(2001, 1001);
        Assert.That(result.IsT0, Is.True, "Expected ErrNotManager");
    }

    [Test]
    public async Task StopBacking_DoesNotResignManager()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 10000, managerId: 2001);
            await WaifuTestHelper.CreateFan(ctx, 2001, 1001);
        }

        var result = await _svc.StopBeingFanAsync(2001);
        Assert.That(result.IsT1, Is.True, "Expected Success");

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.ManagerUserId, Is.EqualTo(2001));
        }

        await _cs.DidNotReceive().AddAsync(Arg.Any<ulong>(), Arg.Any<long>(), Arg.Any<TxData?>());
    }

    [Test]
    public async Task BuyManager_MultipleWaifus_Success()
    {
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 1000);
            await WaifuTestHelper.CreateWaifu(ctx, 1002, price: 1000);
        }

        var result1 = await _svc.BuyManagerAsync(2001, 1001, 1150);
        Assert.That(result1.IsT5, Is.True, "Expected Success for first waifu");

        var result2 = await _svc.BuyManagerAsync(2001, 1002, 1150);
        Assert.That(result2.IsT5, Is.True, "Expected Success for second waifu");

        await using (var ctx = _db.GetDbContext())
        {
            var wi1 = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            var wi2 = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1002);
            Assert.That(wi1.ManagerUserId, Is.EqualTo(2001));
            Assert.That(wi2.ManagerUserId, Is.EqualTo(2001));
        }
    }

    [Test]
    public async Task BuyManager_Overpay_UsesFullBidAmount()
    {
        // Price=1000, bid=5000 (overpaying)
        // Waifu gets 95% of 5000 = 4500+250 = 4750
        await using (var ctx = _db.GetDbContext())
        {
            await WaifuTestHelper.CreateWaifu(ctx, 1001, price: 1000);
        }

        var result = await _svc.BuyManagerAsync(2001, 1001, 5000);
        Assert.That(result.IsT5, Is.True);

        var info = result.AsT5.Value;
        Assert.That(info.PricePaid, Is.EqualTo(5000));
        Assert.That(info.WaifuPayout, Is.EqualTo(4750));
        Assert.That(info.Burned, Is.EqualTo(250));

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.Price, Is.EqualTo(5000));
        }
    }
}
