#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.Premium;

// ─── DB Model ────────────────────────────────
public class PremiumGuild : DbEntity
{
    public ulong GuildId { get; set; }
    public string Tier { get; set; } = "Free";
    public DateTime? ExpiresAt { get; set; }
    public string Features { get; set; } = "";
    public int MaxCustomCommands { get; set; } = 5;
    public int MaxSavedEmbeds { get; set; } = 3;
    public int MaxFeeds { get; set; } = 2;
    public DateTime? PremiumSince { get; set; }
}

// ─── Tier Definitions ────────────────────────
public static class PremiumTiers
{
    public const string Free = "Free";
    public const string Basic = "Basic";
    public const string Pro = "Pro";
    public const string Enterprise = "Enterprise";

    public static readonly string[] AllTiers = [Free, Basic, Pro, Enterprise];

    public static (int customCommands, int savedEmbeds, int feeds) GetLimits(string tier)
        => tier switch
        {
            Basic => (25, 15, 10),
            Pro => (100, 50, 50),
            Enterprise => (int.MaxValue, int.MaxValue, int.MaxValue),
            _ => (5, 3, 2) // Free
        };

    public static string GetTierEmoji(string tier)
        => tier switch
        {
            Basic => "\ud83e\udd49",      // bronze medal
            Pro => "\ud83e\udd48",         // silver medal
            Enterprise => "\ud83e\udd47",  // gold medal
            _ => "\u2b50"                  // star
        };
}

// ─── Premium Features ────────────────────────
public static class PremiumFeatures
{
    public const string CustomBotAvatar = "custom_bot_avatar";
    public const string UnlimitedCommands = "unlimited_commands";
    public const string PrioritySupport = "priority_support";
    public const string WhiteLabel = "white_label";
    public const string GlobalXp = "global_xp";
    public const string AdvancedAnalytics = "advanced_analytics";
    public const string CalamityBoss = "calamity_boss";
    public const string CustomThemes = "custom_themes";

    public static readonly (string feature, string displayName, string description, string minTier)[] All =
    [
        (CustomBotAvatar, "Custom Bot Avatar", "Set a custom avatar for the bot in your server", PremiumTiers.Basic),
        (UnlimitedCommands, "Unlimited Commands", "Remove the custom command limit entirely", PremiumTiers.Enterprise),
        (PrioritySupport, "Priority Support", "Get faster response times from support", PremiumTiers.Basic),
        (WhiteLabel, "White Label", "Remove SantiBot branding from embeds and messages", PremiumTiers.Enterprise),
        (GlobalXp, "Global XP", "Share XP across all servers using SantiBot", PremiumTiers.Pro),
        (AdvancedAnalytics, "Advanced Analytics", "Detailed server analytics and insights dashboard", PremiumTiers.Pro),
        (CalamityBoss, "Calamity Boss", "Access to the Calamity Boss raid mini-game", PremiumTiers.Pro),
        (CustomThemes, "Custom Themes", "Custom embed colors and theme options", PremiumTiers.Basic)
    ];

    public static int TierRank(string tier)
        => tier switch
        {
            PremiumTiers.Basic => 1,
            PremiumTiers.Pro => 2,
            PremiumTiers.Enterprise => 3,
            _ => 0
        };

    public static bool TierHasFeature(string currentTier, string requiredTier)
        => TierRank(currentTier) >= TierRank(requiredTier);
}

// ─── Service ─────────────────────────────────
public sealed class PremiumService : INService
{
    private readonly DbService _db;

    // Cache: GuildId -> PremiumGuild
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, PremiumGuild> _cache = new();

    public PremiumService(DbService db)
    {
        _db = db;
    }

    public async Task<PremiumGuild> GetOrCreateAsync(ulong guildId)
    {
        if (_cache.TryGetValue(guildId, out var cached))
        {
            // Check expiration
            if (cached.ExpiresAt.HasValue && cached.ExpiresAt.Value < DateTime.UtcNow)
            {
                cached.Tier = PremiumTiers.Free;
                var (cmds, embeds, feeds) = PremiumTiers.GetLimits(PremiumTiers.Free);
                cached.MaxCustomCommands = cmds;
                cached.MaxSavedEmbeds = embeds;
                cached.MaxFeeds = feeds;
                cached.Features = "";
                await SaveAsync(cached);
            }
            return cached;
        }

        await using var ctx = _db.GetDbContext();
        var pg = await ctx.GetTable<PremiumGuild>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (pg is null)
        {
            pg = new PremiumGuild
            {
                GuildId = guildId,
                Tier = PremiumTiers.Free,
                MaxCustomCommands = 5,
                MaxSavedEmbeds = 3,
                MaxFeeds = 2
            };

            pg.Id = await ctx.GetTable<PremiumGuild>()
                .InsertWithInt32IdentityAsync(() => new PremiumGuild
                {
                    GuildId = guildId,
                    Tier = PremiumTiers.Free,
                    Features = "",
                    MaxCustomCommands = 5,
                    MaxSavedEmbeds = 3,
                    MaxFeeds = 2,
                    DateAdded = DateTime.UtcNow
                });
        }

        _cache[guildId] = pg;
        return pg;
    }

    public async Task<bool> IsPremiumAsync(ulong guildId)
    {
        var pg = await GetOrCreateAsync(guildId);
        return pg.Tier != PremiumTiers.Free
               && (!pg.ExpiresAt.HasValue || pg.ExpiresAt.Value > DateTime.UtcNow);
    }

    public async Task<string> GetTierAsync(ulong guildId)
    {
        var pg = await GetOrCreateAsync(guildId);
        return pg.Tier;
    }

    public async Task<bool> HasFeatureAsync(ulong guildId, string feature)
    {
        var pg = await GetOrCreateAsync(guildId);

        // Check if explicitly enabled
        var enabledFeatures = pg.Features?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (enabledFeatures.Contains(feature))
            return true;

        // Check if tier grants it
        var featureDef = PremiumFeatures.All.FirstOrDefault(f => f.feature == feature);
        if (featureDef.feature is null)
            return false;

        return PremiumFeatures.TierHasFeature(pg.Tier, featureDef.minTier);
    }

    public async Task<(bool withinLimit, int current, int max)> CheckLimitAsync(
        ulong guildId, string limitType, int currentCount)
    {
        var pg = await GetOrCreateAsync(guildId);
        var max = limitType switch
        {
            "custom_commands" => pg.MaxCustomCommands,
            "saved_embeds" => pg.MaxSavedEmbeds,
            "feeds" => pg.MaxFeeds,
            _ => 0
        };

        // Enterprise = unlimited
        if (pg.Tier == PremiumTiers.Enterprise)
            return (true, currentCount, int.MaxValue);

        return (currentCount < max, currentCount, max);
    }

    public async Task SetTierAsync(ulong guildId, string tier, DateTime? expiresAt = null)
    {
        if (!PremiumTiers.AllTiers.Contains(tier))
            return;

        var pg = await GetOrCreateAsync(guildId);
        pg.Tier = tier;
        pg.ExpiresAt = expiresAt;
        pg.PremiumSince ??= DateTime.UtcNow;

        var (cmds, embeds, feeds) = PremiumTiers.GetLimits(tier);
        pg.MaxCustomCommands = cmds;
        pg.MaxSavedEmbeds = embeds;
        pg.MaxFeeds = feeds;

        // Grant tier features
        var tierFeatures = PremiumFeatures.All
            .Where(f => PremiumFeatures.TierHasFeature(tier, f.minTier))
            .Select(f => f.feature)
            .ToList();

        pg.Features = string.Join(",", tierFeatures);

        await SaveAsync(pg);
        _cache[guildId] = pg;
    }

    public async Task AddFeatureAsync(ulong guildId, string feature)
    {
        var pg = await GetOrCreateAsync(guildId);
        var features = pg.Features?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

        if (!features.Contains(feature))
        {
            features.Add(feature);
            pg.Features = string.Join(",", features);
            await SaveAsync(pg);
        }
    }

    public async Task RemoveFeatureAsync(ulong guildId, string feature)
    {
        var pg = await GetOrCreateAsync(guildId);
        var features = pg.Features?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

        if (features.Remove(feature))
        {
            pg.Features = string.Join(",", features);
            await SaveAsync(pg);
        }
    }

    private async Task SaveAsync(PremiumGuild pg)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<PremiumGuild>()
            .Where(x => x.GuildId == pg.GuildId)
            .UpdateAsync(x => new PremiumGuild
            {
                Tier = pg.Tier,
                ExpiresAt = pg.ExpiresAt,
                Features = pg.Features,
                MaxCustomCommands = pg.MaxCustomCommands,
                MaxSavedEmbeds = pg.MaxSavedEmbeds,
                MaxFeeds = pg.MaxFeeds,
                PremiumSince = pg.PremiumSince
            });

        _cache[pg.GuildId] = pg;
    }

    // ─── Display Helpers ──────────────────────

    public static string FormatTierComparison()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("```");
        sb.AppendLine("Feature            | Free | Basic | Pro  | Enterprise");
        sb.AppendLine("───────────────────┼──────┼───────┼──────┼───────────");
        sb.AppendLine("Custom Commands    |   5  |   25  | 100  | Unlimited");
        sb.AppendLine("Saved Embeds       |   3  |   15  |  50  | Unlimited");
        sb.AppendLine("RSS Feeds          |   2  |   10  |  50  | Unlimited");
        sb.AppendLine("Custom Bot Avatar  |  No  |  Yes  | Yes  |    Yes");
        sb.AppendLine("Priority Support   |  No  |  Yes  | Yes  |    Yes");
        sb.AppendLine("Custom Themes      |  No  |  Yes  | Yes  |    Yes");
        sb.AppendLine("Global XP          |  No  |   No  | Yes  |    Yes");
        sb.AppendLine("Advanced Analytics |  No  |   No  | Yes  |    Yes");
        sb.AppendLine("Calamity Boss      |  No  |   No  | Yes  |    Yes");
        sb.AppendLine("White Label        |  No  |   No  |  No  |    Yes");
        sb.AppendLine("Unlimited Commands |  No  |   No  |  No  |    Yes");
        sb.AppendLine("```");

        return sb.ToString();
    }

    public static string FormatFeatureList()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var (feature, displayName, description, minTier) in PremiumFeatures.All)
        {
            var emoji = PremiumTiers.GetTierEmoji(minTier);
            sb.AppendLine($"{emoji} **{displayName}** (_{minTier}+_)");
            sb.AppendLine($"  {description}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
