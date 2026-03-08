#nullable enable
using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Nadeko.Common;
using NadekoBot.Modules.Waifus.WaifusHubbies;
using NadekoBot.Modules.Waifus.WaifusHubbies.Db;
using NadekoBot.Services;
using NadekoBot.Services.Currency;
using NSubstitute;
using NUnit.Framework;
using OneOf.Types;

namespace NadekoBot.Tests.Waifu;

[TestFixture]
public class WnHMoodActionTests
{
    private WnHService _svc = null!;
    private IBotCache _cache = null!;
    private TestDbService _db = null!;

    [SetUp]
    public void Setup()
    {
        _db = new TestDbService();
        var cs = Substitute.For<ICurrencyService>();
        _cache = Substitute.For<IBotCache>();
        var time = new FakeTimeProvider(new(2025, 1, 7, 0, 0, 0, TimeSpan.Zero));
        var client = Substitute.For<DiscordSocketClient>();
        _svc = new WnHService(_db, _cache, cs, client, null!, time);
        _cache.GetAsync(Arg.Any<TypedKey<int>>())
            .Returns(new System.Threading.Tasks.ValueTask<OneOf.OneOf<int, None>>(new None()));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task ImproveMood_AllActions_CorrectMoodIncrease()
    {
        // Hug=50, Kiss=75, Pat=37
        await using (var ctx = _db.GetDbContext())
        {
            await WnHTestHelper.CreateWaifu(ctx, 1001, mood: 0);
            await WnHTestHelper.CreateWaifu(ctx, 1002, mood: 0);
            await WnHTestHelper.CreateWaifu(ctx, 1003, mood: 0);
        }

        await _svc.ImproveMoodAsync(2001, 1001, WaifuAction.Hug);
        await _svc.ImproveMoodAsync(2002, 1002, WaifuAction.Kiss);
        await _svc.ImproveMoodAsync(2003, 1003, WaifuAction.Pat);

        await using (var ctx = _db.GetDbContext())
        {
            Assert.That((await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001)).Mood, Is.EqualTo(50));
            Assert.That((await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1002)).Mood, Is.EqualTo(75));
            Assert.That((await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1003)).Mood, Is.EqualTo(37));
        }
    }

    [Test]
    public async Task ImproveMood_CappedAt1000()
    {
        await using (var ctx = _db.GetDbContext())
            await WnHTestHelper.CreateWaifu(ctx, 1001, mood: 980);

        await _svc.ImproveMoodAsync(2001, 1001, WaifuAction.Hug);

        await using (var ctx = _db.GetDbContext())
        {
            var wi = await ctx.GetTable<WaifuInfo>().FirstAsyncLinqToDB(x => x.UserId == 1001);
            Assert.That(wi.Mood, Is.EqualTo(1000));
        }
    }

    [Test]
    public async Task ImproveMood_NoActionsLeft_ReturnsError()
    {
        _cache.GetAsync(Arg.Any<TypedKey<int>>())
            .Returns(new System.Threading.Tasks.ValueTask<OneOf.OneOf<int, None>>((OneOf.OneOf<int, None>)2));

        await using (var ctx = _db.GetDbContext())
            await WnHTestHelper.CreateWaifu(ctx, 1001, mood: 500);

        var result = await _svc.ImproveMoodAsync(2001, 1001, WaifuAction.Hug);
        Assert.That(result.IsT1, Is.True, "Expected ErrNoActionsLeft");
    }
}
