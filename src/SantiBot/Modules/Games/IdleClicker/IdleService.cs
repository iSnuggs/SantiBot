#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Games.IdleClicker;

public sealed class IdleService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    public static readonly Dictionary<string, (long Cost, double RpsBoost, int ClickBoost)> Upgrades = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AutoClicker"] = (100, 0.1, 0),
        ["BetterFinger"] = (50, 0, 2),
        ["Warehouse"] = (200, 0.5, 0),
        ["Factory"] = (500, 2.0, 0),
        ["RoboArm"] = (150, 0, 5),
        ["MegaClick"] = (300, 0, 10),
        ["Turbine"] = (1000, 5.0, 0),
        ["QuantumCore"] = (2500, 15.0, 0),
    };

    public IdleService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    private async Task<IdlePlayer> GetOrCreatePlayer(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var player = await ctx.GetTable<IdlePlayer>()
            .FirstOrDefaultAsyncLinqToDB(p => p.UserId == userId);

        if (player is null)
        {
            await ctx.GetTable<IdlePlayer>().InsertAsync(() => new IdlePlayer
            {
                UserId = userId,
                Resources = 0,
                ResourcesPerSecond = 0.1,
                ClickPower = 1,
                PrestigeLevel = 0,
                PrestigeMultiplier = 1.0,
                LastCollected = DateTime.UtcNow,
                Upgrades = "",
                DateAdded = DateTime.UtcNow
            });
            player = await ctx.GetTable<IdlePlayer>()
                .FirstOrDefaultAsyncLinqToDB(p => p.UserId == userId);
        }

        return player;
    }

    private long CalculateOfflineEarnings(IdlePlayer player)
    {
        var seconds = (DateTime.UtcNow - player.LastCollected).TotalSeconds;
        // Cap at 24 hours of offline earnings
        seconds = Math.Min(seconds, 86400);
        return (long)(seconds * player.ResourcesPerSecond * player.PrestigeMultiplier);
    }

    public async Task<(IdlePlayer Player, long OfflineEarnings)> GetStatusAsync(ulong userId)
    {
        var player = await GetOrCreatePlayer(userId);
        var offline = CalculateOfflineEarnings(player);
        return (player, offline);
    }

    public async Task<(bool Success, string Message, long Earned)> ClickAsync(ulong userId)
    {
        var player = await GetOrCreatePlayer(userId);
        var offline = CalculateOfflineEarnings(player);
        var clickEarning = (long)(player.ClickPower * player.PrestigeMultiplier);
        var total = offline + clickEarning;

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<IdlePlayer>()
            .Where(p => p.Id == player.Id)
            .UpdateAsync(p => new IdlePlayer
            {
                Resources = player.Resources + total,
                LastCollected = DateTime.UtcNow
            });

        return (true, $"Click! +{clickEarning} resources" + (offline > 0 ? $" (+{offline} offline earnings)" : ""), total);
    }

    public async Task<(bool Success, string Message)> BuyUpgradeAsync(ulong userId, string upgradeName)
    {
        if (!Upgrades.TryGetValue(upgradeName, out var info))
            return (false, $"Unknown upgrade. Available: {string.Join(", ", Upgrades.Keys)}");

        var player = await GetOrCreatePlayer(userId);

        // Get current level of this upgrade
        var upgradeDict = ParseUpgrades(player.Upgrades);
        var currentLevel = upgradeDict.GetValueOrDefault(upgradeName, 0);
        var cost = info.Cost * (currentLevel + 1); // Each level costs more

        // Collect offline earnings first
        var offline = CalculateOfflineEarnings(player);
        var totalResources = player.Resources + offline;

        if (totalResources < cost)
            return (false, $"Need {cost} resources! You have {totalResources}.");

        upgradeDict[upgradeName] = currentLevel + 1;
        var newUpgrades = string.Join(",", upgradeDict.Select(kv => $"{kv.Key}:{kv.Value}"));

        var newRps = player.ResourcesPerSecond + info.RpsBoost;
        var newClick = player.ClickPower + info.ClickBoost;

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<IdlePlayer>()
            .Where(p => p.Id == player.Id)
            .UpdateAsync(p => new IdlePlayer
            {
                Resources = totalResources - cost,
                ResourcesPerSecond = newRps,
                ClickPower = newClick,
                Upgrades = newUpgrades,
                LastCollected = DateTime.UtcNow
            });

        return (true, $"Bought **{upgradeName}** (Lv{currentLevel + 1})! Cost: {cost} resources.\nRPS: {newRps:F1} | Click: {newClick}");
    }

    public async Task<(bool Success, string Message)> PrestigeAsync(ulong userId)
    {
        var player = await GetOrCreatePlayer(userId);
        var offline = CalculateOfflineEarnings(player);
        var total = player.Resources + offline;

        if (total < 10000)
            return (false, $"Need 10,000 resources to prestige! You have {total}.");

        var newPrestige = player.PrestigeLevel + 1;
        var newMult = 1.0 + newPrestige * 0.5; // Each prestige adds 50% multiplier

        // Convert some resources to currency
        var currencyReward = total / 100;
        await _cs.AddAsync(userId, currencyReward, new TxData("idle", "prestige"));

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<IdlePlayer>()
            .Where(p => p.Id == player.Id)
            .UpdateAsync(p => new IdlePlayer
            {
                Resources = 0,
                ResourcesPerSecond = 0.1,
                ClickPower = 1,
                PrestigeLevel = newPrestige,
                PrestigeMultiplier = newMult,
                Upgrades = "",
                LastCollected = DateTime.UtcNow
            });

        return (true, $"🌟 **Prestige {newPrestige}!** Multiplier: {newMult:F1}x\nAll upgrades reset. Converted {currencyReward} 🥠!");
    }

    private Dictionary<string, int> ParseUpgrades(string upgrades)
    {
        if (string.IsNullOrWhiteSpace(upgrades))
            return new();

        return upgrades.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Split(':'))
            .Where(parts => parts.Length == 2 && int.TryParse(parts[1], out _))
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1]));
    }
}
