#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Games.CardCollecting;

public sealed class CardService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    private static readonly (string Name, string Set, string Rarity)[] AllCards =
    [
        // Nature Set
        ("Mountain Peak", "Nature", "Common"),
        ("Forest Stream", "Nature", "Common"),
        ("Desert Sunset", "Nature", "Common"),
        ("Ocean Wave", "Nature", "Uncommon"),
        ("Northern Lights", "Nature", "Uncommon"),
        ("Volcanic Eruption", "Nature", "Rare"),
        ("Aurora Borealis", "Nature", "Epic"),
        ("World Tree", "Nature", "Legendary"),
        // Mythical Set
        ("Phoenix Feather", "Mythical", "Common"),
        ("Dragon Scale", "Mythical", "Common"),
        ("Unicorn Horn", "Mythical", "Uncommon"),
        ("Griffin Claw", "Mythical", "Uncommon"),
        ("Hydra Fang", "Mythical", "Rare"),
        ("Kraken Tentacle", "Mythical", "Rare"),
        ("Leviathan Eye", "Mythical", "Epic"),
        ("Celestial Dragon", "Mythical", "Legendary"),
        // Heroes Set
        ("Warrior Shield", "Heroes", "Common"),
        ("Mage Staff", "Heroes", "Common"),
        ("Rogue Dagger", "Heroes", "Common"),
        ("Paladin Helm", "Heroes", "Uncommon"),
        ("Necromancer Tome", "Heroes", "Uncommon"),
        ("Berserker Axe", "Heroes", "Rare"),
        ("Archmage Crown", "Heroes", "Epic"),
        ("Legendary Blade", "Heroes", "Legendary"),
        // Space Set
        ("Asteroid Belt", "Space", "Common"),
        ("Nebula Cloud", "Space", "Common"),
        ("Red Giant Star", "Space", "Uncommon"),
        ("Black Hole", "Space", "Rare"),
        ("Supernova", "Space", "Rare"),
        ("Wormhole", "Space", "Epic"),
        ("Galaxy Core", "Space", "Epic"),
        ("Big Bang", "Space", "Legendary"),
    ];

    private static readonly Dictionary<string, int> RarityWeights = new()
    {
        ["Common"] = 50,
        ["Uncommon"] = 30,
        ["Rare"] = 13,
        ["Epic"] = 5,
        ["Legendary"] = 2,
    };

    public CardService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public async Task<(string Name, string Set, string Rarity)> DrawDailyCardAsync(ulong userId)
    {
        // Pick rarity
        var totalWeight = RarityWeights.Values.Sum();
        var roll = _rng.Next(totalWeight);
        var cumulative = 0;
        string chosenRarity = "Common";

        foreach (var (rarity, weight) in RarityWeights)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                chosenRarity = rarity;
                break;
            }
        }

        var cardsOfRarity = AllCards.Where(c => c.Rarity == chosenRarity).ToArray();
        var chosen = cardsOfRarity[_rng.Next(cardsOfRarity.Length)];

        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<CollectibleCard>()
            .FirstOrDefaultAsyncLinqToDB(c => c.UserId == userId && c.CardName == chosen.Name);

        if (existing is not null)
        {
            await ctx.GetTable<CollectibleCard>()
                .Where(c => c.Id == existing.Id)
                .UpdateAsync(c => new CollectibleCard { Quantity = existing.Quantity + 1 });
        }
        else
        {
            await ctx.GetTable<CollectibleCard>().InsertAsync(() => new CollectibleCard
            {
                UserId = userId,
                CardName = chosen.Name,
                Rarity = chosen.Rarity,
                Set = chosen.Set,
                Quantity = 1,
                DateAdded = DateTime.UtcNow
            });
        }

        return chosen;
    }

    public async Task<List<CollectibleCard>> GetInventoryAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<CollectibleCard>()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Set)
            .ThenBy(c => c.CardName)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool Success, string Message)> TradeCardAsync(ulong fromUser, ulong toUser, string offerCardName, string wantCardName)
    {
        await using var ctx = _db.GetDbContext();

        var offerCard = await ctx.GetTable<CollectibleCard>()
            .FirstOrDefaultAsyncLinqToDB(c => c.UserId == fromUser && c.CardName == offerCardName && c.Quantity > 0);

        var wantCard = await ctx.GetTable<CollectibleCard>()
            .FirstOrDefaultAsyncLinqToDB(c => c.UserId == toUser && c.CardName == wantCardName && c.Quantity > 0);

        if (offerCard is null)
            return (false, $"You don't have {offerCardName}!");

        if (wantCard is null)
            return (false, $"They don't have {wantCardName}!");

        // Remove from both
        if (offerCard.Quantity <= 1)
            await ctx.GetTable<CollectibleCard>().DeleteAsync(c => c.Id == offerCard.Id);
        else
            await ctx.GetTable<CollectibleCard>().Where(c => c.Id == offerCard.Id)
                .UpdateAsync(c => new CollectibleCard { Quantity = offerCard.Quantity - 1 });

        if (wantCard.Quantity <= 1)
            await ctx.GetTable<CollectibleCard>().DeleteAsync(c => c.Id == wantCard.Id);
        else
            await ctx.GetTable<CollectibleCard>().Where(c => c.Id == wantCard.Id)
                .UpdateAsync(c => new CollectibleCard { Quantity = wantCard.Quantity - 1 });

        // Add to both
        await AddCardToUser(ctx, fromUser, wantCard);
        await AddCardToUser(ctx, toUser, offerCard);

        return (true, $"Trade complete! {offerCardName} <-> {wantCardName}");
    }

    private async Task AddCardToUser(SantiBot.Db.SantiContext ctx, ulong userId, CollectibleCard card)
    {
        var existing = await ctx.GetTable<CollectibleCard>()
            .FirstOrDefaultAsyncLinqToDB(c => c.UserId == userId && c.CardName == card.CardName);

        if (existing is not null)
        {
            await ctx.GetTable<CollectibleCard>()
                .Where(c => c.Id == existing.Id)
                .UpdateAsync(c => new CollectibleCard { Quantity = existing.Quantity + 1 });
        }
        else
        {
            await ctx.GetTable<CollectibleCard>().InsertAsync(() => new CollectibleCard
            {
                UserId = userId,
                CardName = card.CardName,
                Rarity = card.Rarity,
                Set = card.Set,
                Quantity = 1,
                DateAdded = DateTime.UtcNow
            });
        }
    }

    public Dictionary<string, (int Have, int Total)> GetAlbumProgress(List<CollectibleCard> cards)
    {
        var sets = AllCards.GroupBy(c => c.Set).ToDictionary(g => g.Key, g => g.Count());
        var result = new Dictionary<string, (int Have, int Total)>();

        foreach (var (setName, total) in sets)
        {
            var have = cards.Count(c => c.Set == setName);
            result[setName] = (have, total);
        }

        return result;
    }

    public static (string Name, string Set, string Rarity)[] GetAllCards() => AllCards;
}
