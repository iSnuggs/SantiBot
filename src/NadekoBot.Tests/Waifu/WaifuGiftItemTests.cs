#nullable enable
using System;
using System.Linq;
using NadekoBot.Modules.Waifus.WaifusHubbies;
using NUnit.Framework;

namespace NadekoBot.Tests.Waifu;

[TestFixture]
public class WaifuGiftItemTests
{
    [Test]
    public void AllItems_CorrectStructure_24Items_6Tiers()
    {
        Assert.That(WaifuGiftItems.AllItems, Has.Count.EqualTo(24));
        Assert.That(WaifuGiftItems.AllItems.Count(x => x.Type == GiftItemType.Food), Is.EqualTo(12));
        Assert.That(WaifuGiftItems.AllItems.Count(x => x.Type == GiftItemType.Mood), Is.EqualTo(12));

        long[] tiers = [10, 50, 250, 1000, 2500, 5000];
        var distinctPrices = WaifuGiftItems.AllItems.Select(x => x.Price).Distinct().OrderBy(x => x).ToList();
        Assert.That(distinctPrices, Is.EqualTo(tiers));

        foreach (var price in tiers)
        {
            var items = WaifuGiftItems.AllItems.Where(x => x.Price == price).ToList();
            Assert.That(items, Has.Count.EqualTo(4), $"Expected 4 items at tier {price}");
        }
    }

    [Test]
    public void GetTodaysItems_6Items_3FoodAnd3Mood_AllTiers()
    {
        var today = WaifuGiftItems.GetTodaysItems();
        Assert.That(today, Has.Count.EqualTo(6));
        Assert.That(today.Count(x => x.Type == GiftItemType.Food), Is.EqualTo(3));
        Assert.That(today.Count(x => x.Type == GiftItemType.Mood), Is.EqualTo(3));

        var prices = today.Select(x => x.Price).OrderBy(x => x).ToList();
        Assert.That(prices, Is.EqualTo(new long[] { 10, 50, 250, 1000, 2500, 5000 }));

        // Deterministic
        var second = WaifuGiftItems.GetTodaysItems();
        Assert.That(today.Select(x => x.Id), Is.EqualTo(second.Select(x => x.Id)));
    }

    [Test]
    public void FindTodaysItem_CaseInsensitive_ReturnsNullForMissing()
    {
        var first = WaifuGiftItems.GetTodaysItems()[0];

        Assert.That(WaifuGiftItems.FindTodaysItem(first.Name.ToUpperInvariant())?.Id, Is.EqualTo(first.Id));
        Assert.That(WaifuGiftItems.FindTodaysItem("NonExistentItemXYZ"), Is.Null);
        Assert.That(WaifuGiftItems.FindTodaysItemById(Guid.NewGuid()), Is.Null);
    }
}
