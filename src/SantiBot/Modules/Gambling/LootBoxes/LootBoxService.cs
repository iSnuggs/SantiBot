#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.LootBoxes;

public sealed class LootBoxService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    private const long BOX_PRICE = 200;

    // Tier -> (chance weight, currency multiplier min, currency multiplier max)
    // Balanced for ~85% expected return (healthy currency sink)
    public static readonly (string Name, int Weight, double MinMult, double MaxMult)[] Tiers =
    [
        ("Common", 60, 0.1, 0.6),
        ("Uncommon", 25, 0.6, 1.2),
        ("Rare", 10, 1.2, 2.5),
        ("Legendary", 4, 2.5, 5.0),
        ("Mythic", 1, 5.0, 12.0),
    ];

    public LootBoxService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    private async Task<UserLootBox> GetOrCreateInventory(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var inv = await ctx.GetTable<UserLootBox>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);

        if (inv is null)
        {
            await ctx.GetTable<UserLootBox>().InsertAsync(() => new UserLootBox
            {
                GuildId = guildId,
                UserId = userId,
                UnopenedBoxes = 0,
                CommonBoxes = 0,
                UncommonBoxes = 0,
                RareBoxes = 0,
                LegendaryBoxes = 0,
                MythicBoxes = 0,
                DateAdded = DateTime.UtcNow
            });
            inv = await ctx.GetTable<UserLootBox>()
                .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);
        }

        return inv;
    }

    public async Task<(bool Success, string Message)> BuyBoxesAsync(ulong guildId, ulong userId, int amount)
    {
        if (amount < 1 || amount > 50)
            return (false, "Buy between 1 and 50 boxes!");

        var cost = BOX_PRICE * amount;
        var removed = await _cs.RemoveAsync(userId, cost, new TxData("lootbox", "buy"));
        if (!removed)
            return (false, $"You need {cost} 🥠 ({BOX_PRICE} 🥠 each)!");

        var inv = await GetOrCreateInventory(guildId, userId);
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<UserLootBox>()
            .Where(x => x.Id == inv.Id)
            .UpdateAsync(x => new UserLootBox { UnopenedBoxes = inv.UnopenedBoxes + amount });

        return (true, $"Bought {amount} loot box(es) for {cost} 🥠! You now have {inv.UnopenedBoxes + amount} unopened.");
    }

    public async Task<(bool Success, string Message, List<(string Tier, long Reward)> Results)> OpenBoxesAsync(ulong guildId, ulong userId, int amount)
    {
        if (amount < 1 || amount > 20)
            return (false, "Open between 1 and 20 boxes at a time!", null);

        // Atomic: only subtract if enough boxes exist (prevents race condition)
        await using var atomicCtx = _db.GetDbContext();
        var rowsAffected = await atomicCtx.GetTable<UserLootBox>()
            .Where(x => x.GuildId == guildId && x.UserId == userId && x.UnopenedBoxes >= amount)
            .UpdateAsync(x => new UserLootBox { UnopenedBoxes = x.UnopenedBoxes - amount });

        if (rowsAffected == 0)
            return (false, "You don't have enough unopened boxes!", null);

        var inv = await GetOrCreateInventory(guildId, userId);

        var results = new List<(string Tier, long Reward)>();
        long totalReward = 0;
        int common = 0, uncommon = 0, rare = 0, legendary = 0, mythic = 0;

        var totalWeight = Tiers.Sum(t => t.Weight);

        for (int i = 0; i < amount; i++)
        {
            var roll = _rng.Next(0, totalWeight);
            var cumulative = 0;
            foreach (var tier in Tiers)
            {
                cumulative += tier.Weight;
                if (roll < cumulative)
                {
                    var mult = tier.MinMult + _rng.NextDouble() * (tier.MaxMult - tier.MinMult);
                    var reward = (long)(BOX_PRICE * mult);
                    results.Add((tier.Name, reward));
                    totalReward += reward;

                    switch (tier.Name)
                    {
                        case "Common": common++; break;
                        case "Uncommon": uncommon++; break;
                        case "Rare": rare++; break;
                        case "Legendary": legendary++; break;
                        case "Mythic": mythic++; break;
                    }
                    break;
                }
            }
        }

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<UserLootBox>()
            .Where(x => x.Id == inv.Id)
            .UpdateAsync(x => new UserLootBox
            {
                CommonBoxes = inv.CommonBoxes + common,
                UncommonBoxes = inv.UncommonBoxes + uncommon,
                RareBoxes = inv.RareBoxes + rare,
                LegendaryBoxes = inv.LegendaryBoxes + legendary,
                MythicBoxes = inv.MythicBoxes + mythic
            });

        await _cs.AddAsync(userId, totalReward, new TxData("lootbox", "open"));

        return (true, $"Opened {amount} boxes! Total reward: **{totalReward}** 🥠", results);
    }

    public async Task<UserLootBox> GetInventoryAsync(ulong guildId, ulong userId)
    {
        return await GetOrCreateInventory(guildId, userId);
    }
}
