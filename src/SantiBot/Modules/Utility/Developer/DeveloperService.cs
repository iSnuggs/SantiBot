#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.Developer;

public sealed class DeveloperService : INService
{
    private readonly DbService _db;
    private static readonly SantiRandom _rng = new();

    // Feature flags cache
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong GuildId, string Feature), bool> _flagCache = new();

    public DeveloperService(DbService db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════════════════
    //  FEATURE FLAGS
    // ═══════════════════════════════════════════════════════════

    public async Task<FeatureFlag> SetFlagAsync(ulong guildId, string featureName, bool enabled, int rolloutPercent = 100)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<FeatureFlag>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.FeatureName == featureName);

        if (existing is not null)
        {
            await ctx.GetTable<FeatureFlag>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new FeatureFlag { IsEnabled = enabled, RolloutPercent = rolloutPercent });
            _flagCache[(guildId, featureName)] = enabled;
            return existing;
        }

        var flag = new FeatureFlag
        {
            GuildId = guildId, FeatureName = featureName,
            IsEnabled = enabled, RolloutPercent = rolloutPercent,
        };
        ctx.Add(flag);
        await ctx.SaveChangesAsync();
        _flagCache[(guildId, featureName)] = enabled;
        return flag;
    }

    public async Task<bool> IsFlagEnabledAsync(ulong guildId, string featureName)
    {
        if (_flagCache.TryGetValue((guildId, featureName), out var cached))
            return cached;

        await using var ctx = _db.GetDbContext();
        var flag = await ctx.GetTable<FeatureFlag>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.FeatureName == featureName);

        var result = flag?.IsEnabled ?? true;
        _flagCache[(guildId, featureName)] = result;
        return result;
    }

    public async Task<List<FeatureFlag>> GetAllFlagsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<FeatureFlag>()
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.FeatureName)
            .ToListAsyncLinqToDB();
    }

    // ═══════════════════════════════════════════════════════════
    //  WEBHOOK ENDPOINTS
    // ═══════════════════════════════════════════════════════════

    public async Task<WebhookEndpoint> CreateWebhookAsync(ulong guildId, string name, string targetChannelId, string eventType)
    {
        var secret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await using var ctx = _db.GetDbContext();
        var webhook = new WebhookEndpoint
        {
            GuildId = guildId, Name = name, Secret = secret,
            TargetChannelId = targetChannelId, EventType = eventType,
        };
        ctx.Add(webhook);
        await ctx.SaveChangesAsync();
        return webhook;
    }

    public async Task<List<WebhookEndpoint>> GetWebhooksAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<WebhookEndpoint>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> DeleteWebhookAsync(ulong guildId, int webhookId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<WebhookEndpoint>()
            .Where(x => x.GuildId == guildId && x.Id == webhookId)
            .DeleteAsync() > 0;
    }

    // ═══════════════════════════════════════════════════════════
    //  COMMAND LOGGING
    // ═══════════════════════════════════════════════════════════

    public async Task LogCommandAsync(ulong guildId, ulong userId, ulong channelId, string commandName, string args, bool success, int executionMs)
    {
        await using var ctx = _db.GetDbContext();
        ctx.Add(new CommandLog
        {
            GuildId = guildId, UserId = userId, ChannelId = channelId,
            CommandName = commandName, Arguments = args,
            Success = success, ExecutionMs = executionMs,
        });
        await ctx.SaveChangesAsync();
    }

    public async Task<List<(string Command, int Count)>> GetTopCommandsAsync(ulong guildId, int days = 7)
    {
        await using var ctx = _db.GetDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await ctx.GetTable<CommandLog>()
            .Where(x => x.GuildId == guildId && x.ExecutedAt >= cutoff)
            .GroupBy(x => x.CommandName)
            .Select(g => new { Command = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsyncLinqToDB()
            .ContinueWith(t => t.Result.Select(x => (x.Command, x.Count)).ToList());
    }

    public async Task<(int Total, int Success, int Failed, double AvgMs)> GetCommandStatsAsync(ulong guildId, int days = 7)
    {
        await using var ctx = _db.GetDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var logs = await ctx.GetTable<CommandLog>()
            .Where(x => x.GuildId == guildId && x.ExecutedAt >= cutoff)
            .ToListAsyncLinqToDB();

        if (logs.Count == 0) return (0, 0, 0, 0);
        return (logs.Count, logs.Count(l => l.Success), logs.Count(l => !l.Success), logs.Average(l => l.ExecutionMs));
    }

    // ═══════════════════════════════════════════════════════════
    //  XP MULTIPLIERS
    // ═══════════════════════════════════════════════════════════

    public async Task<XpMultiplier> SetMultiplierAsync(ulong guildId, string type, ulong targetId, double multiplier, DateTime? expiresAt)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<XpMultiplier>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Type == type && x.TargetId == targetId);

        if (existing is not null)
        {
            await ctx.GetTable<XpMultiplier>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new XpMultiplier { Multiplier = multiplier, ExpiresAt = expiresAt, IsActive = true });
            return existing;
        }

        var mult = new XpMultiplier
        {
            GuildId = guildId, Type = type, TargetId = targetId,
            Multiplier = multiplier, ExpiresAt = expiresAt,
        };
        ctx.Add(mult);
        await ctx.SaveChangesAsync();
        return mult;
    }

    public async Task<double> GetEffectiveMultiplierAsync(ulong guildId, ulong channelId, ulong userId, IEnumerable<ulong> roleIds)
    {
        await using var ctx = _db.GetDbContext();
        var now = DateTime.UtcNow;
        var multipliers = await ctx.GetTable<XpMultiplier>()
            .Where(x => x.GuildId == guildId && x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now))
            .ToListAsyncLinqToDB();

        var result = 1.0;
        foreach (var m in multipliers)
        {
            switch (m.Type)
            {
                case "Global":
                case "Event":
                    result *= m.Multiplier;
                    break;
                case "Channel" when m.TargetId == channelId:
                    result *= m.Multiplier;
                    break;
                case "Role" when roleIds.Contains(m.TargetId):
                    result *= m.Multiplier;
                    break;
            }
        }
        return result;
    }

    public async Task<List<XpMultiplier>> GetMultipliersAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<XpMultiplier>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .OrderBy(x => x.Type)
            .ToListAsyncLinqToDB();
    }

    // ═══════════════════════════════════════════════════════════
    //  XP CHALLENGES
    // ═══════════════════════════════════════════════════════════

    public async Task<XpChallengeEntry> CreateChallengeAsync(ulong guildId, string name, string desc, string requirement, long xpReward, int durationDays)
    {
        await using var ctx = _db.GetDbContext();
        var challenge = new XpChallengeEntry
        {
            GuildId = guildId, ChallengeName = name, Description = desc,
            Requirement = requirement, XpReward = xpReward,
            StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddDays(durationDays),
        };
        ctx.Add(challenge);
        await ctx.SaveChangesAsync();
        return challenge;
    }

    public async Task<List<XpChallengeEntry>> GetActiveChallengesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var now = DateTime.UtcNow;
        return await ctx.GetTable<XpChallengeEntry>()
            .Where(x => x.GuildId == guildId && x.IsActive && x.EndsAt > now)
            .ToListAsyncLinqToDB();
    }

    // ═══════════════════════════════════════════════════════════
    //  BOT INFO / CHANGELOG
    // ═══════════════════════════════════════════════════════════

    public static readonly (string Version, string Date, string Changes)[] Changelog =
    [
        ("1.0.0", "2026-03-29", "Initial release — 1000+ features!"),
        ("0.9.0", "2026-03-28", "100 post-launch features, Phases 9-12 complete"),
        ("0.8.0", "2026-03-27", "51 bonus features + Mod Mail system"),
        ("0.7.0", "2026-03-26", "Dashboard with 32 pages + 23 API endpoints"),
        ("0.6.0", "2026-03-25", "Distribution: Docker, Railway, Pterodactyl, Linux"),
        ("0.5.0", "2026-03-24", "13 missing Dyno features + slash commands"),
    ];
}
