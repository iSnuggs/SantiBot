#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.RealEstate;

public sealed class RealEstateService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    public static readonly Dictionary<string, (long Cost, long IncomePerHour)> Properties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Shack"] = (100, 5),
        ["House"] = (500, 20),
        ["Mansion"] = (2000, 75),
        ["Castle"] = (10000, 300),
        ["Skyscraper"] = (50000, 1200),
    };

    public RealEstateService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public async Task<(bool Success, string Message)> BuyPropertyAsync(ulong guildId, ulong userId, string propertyType)
    {
        if (!Properties.TryGetValue(propertyType, out var info))
            return (false, $"Unknown property. Available: {string.Join(", ", Properties.Keys)}");

        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<RealEstateProperty>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId && x.PropertyType == propertyType);

        if (existing is not null)
            return (false, "You already own this property type!");

        var removed = await _cs.RemoveAsync(userId, info.Cost, new TxData("realestate", "buy"));
        if (!removed)
            return (false, $"You need {info.Cost} 🥠 to buy a {propertyType}!");

        await ctx.GetTable<RealEstateProperty>().InsertAsync(() => new RealEstateProperty
        {
            GuildId = guildId,
            UserId = userId,
            PropertyType = propertyType,
            UpgradeLevel = 0,
            LastCollected = DateTime.UtcNow,
            DateAdded = DateTime.UtcNow
        });

        return (true, $"You bought a **{propertyType}** for {info.Cost} 🥠! It earns {info.IncomePerHour} 🥠/hr.");
    }

    public async Task<(bool Success, string Message)> UpgradePropertyAsync(ulong guildId, ulong userId, string propertyType)
    {
        if (!Properties.TryGetValue(propertyType, out var info))
            return (false, "Unknown property type.");

        await using var ctx = _db.GetDbContext();
        var prop = await ctx.GetTable<RealEstateProperty>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId && x.PropertyType == propertyType);

        if (prop is null)
            return (false, "You don't own that property!");

        if (prop.UpgradeLevel >= 3)
            return (false, "Property is already at max level (3)!");

        var upgradeCost = info.Cost * (prop.UpgradeLevel + 1);
        var removed = await _cs.RemoveAsync(userId, upgradeCost, new TxData("realestate", "upgrade"));
        if (!removed)
            return (false, $"Upgrade costs {upgradeCost} 🥠!");

        await ctx.GetTable<RealEstateProperty>()
            .Where(x => x.Id == prop.Id)
            .UpdateAsync(x => new RealEstateProperty { UpgradeLevel = prop.UpgradeLevel + 1 });

        var newIncome = info.IncomePerHour * (long)Math.Pow(2, prop.UpgradeLevel + 1);
        return (true, $"Upgraded **{propertyType}** to level {prop.UpgradeLevel + 1}! Now earns {newIncome} 🥠/hr.");
    }

    public async Task<List<RealEstateProperty>> GetPropertiesAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<RealEstateProperty>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool Success, string Message, long Amount)> CollectIncomeAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var props = await ctx.GetTable<RealEstateProperty>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .ToListAsyncLinqToDB();

        if (props.Count == 0)
            return (false, "You don't own any properties!", 0);

        long totalIncome = 0;
        var now = DateTime.UtcNow;

        foreach (var prop in props)
        {
            if (!Properties.TryGetValue(prop.PropertyType, out var info))
                continue;

            var hours = (now - prop.LastCollected).TotalHours;
            if (hours < 1)
                continue;

            var income = (long)(info.IncomePerHour * Math.Pow(2, prop.UpgradeLevel) * Math.Floor(hours));
            totalIncome += income;

            await ctx.GetTable<RealEstateProperty>()
                .Where(x => x.Id == prop.Id)
                .UpdateAsync(x => new RealEstateProperty { LastCollected = now });
        }

        if (totalIncome <= 0)
            return (false, "No income to collect yet. Come back in an hour!", 0);

        await _cs.AddAsync(userId, totalIncome, new TxData("realestate", "collect"));
        return (true, $"Collected **{totalIncome}** 🥠 from your properties!", totalIncome);
    }

    public async Task<(bool Success, string Message)> SellPropertyAsync(ulong guildId, ulong userId, string propertyType)
    {
        if (!Properties.TryGetValue(propertyType, out var info))
            return (false, "Unknown property type.");

        await using var ctx = _db.GetDbContext();
        var prop = await ctx.GetTable<RealEstateProperty>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId && x.PropertyType == propertyType);

        if (prop is null)
            return (false, "You don't own that property!");

        var sellPrice = info.Cost / 2 * (1 + prop.UpgradeLevel);
        await ctx.GetTable<RealEstateProperty>().DeleteAsync(x => x.Id == prop.Id);
        await _cs.AddAsync(userId, sellPrice, new TxData("realestate", "sell"));

        return (true, $"Sold **{propertyType}** for {sellPrice} 🥠!");
    }
}
