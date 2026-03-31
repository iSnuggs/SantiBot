#nullable disable
using System.Text.RegularExpressions;
using SantiBot.Common.ModuleBehaviors;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.EmbedFix;

/// <summary>
/// Auto-fixes broken embeds from Twitter/X, Instagram, TikTok, Reddit, etc.
/// Replaces links with embed-friendly alternatives and reposts them.
/// </summary>
public sealed class EmbedFixService : INService, IExecOnMessage
{
    private readonly DiscordSocketClient _client;
    private readonly DbService _db;

    // In-memory cache of enabled guilds + their settings
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, EmbedFixSettings> _settings = new();
    private bool _loaded;

    public int Priority => 0;

    // Supported platforms and their fix domains
    public static readonly Dictionary<string, (string Name, string FixDomain, string Emoji)> Platforms = new()
    {
        ["twitter"]   = ("Twitter/X",   "fxtwitter.com",     "🐦"),
        ["instagram"] = ("Instagram",   "ddinstagram.com",   "📸"),
        ["tiktok"]    = ("TikTok",      "vxtiktok.com",      "🎵"),
        ["reddit"]    = ("Reddit",      "rxddit.com",        "🤖"),
        ["bluesky"]   = ("Bluesky",     "fxbsky.app",        "🦋"),
    };

    // Regex patterns for each platform
    private static readonly (string Platform, Regex Pattern)[] LinkPatterns =
    [
        ("twitter",   new Regex(@"https?://(www\.)?(twitter\.com|x\.com)/\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("instagram", new Regex(@"https?://(www\.)?instagram\.com/(p|reel|stories)/\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("tiktok",    new Regex(@"https?://(www\.|vm\.)?tiktok\.com/\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("reddit",    new Regex(@"https?://(www\.|old\.)?reddit\.com/r/\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("bluesky",   new Regex(@"https?://bsky\.app/\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    ];

    public EmbedFixService(DiscordSocketClient client, DbService db)
    {
        _client = client;
        _db = db;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await using var ctx = _db.GetDbContext();
        var configs = await ctx.GetTable<EmbedFixConfig>()
            .ToListAsyncLinqToDB();
        foreach (var c in configs)
            _settings[c.GuildId] = new EmbedFixSettings
            {
                Enabled = c.Enabled,
                DeleteOriginal = c.DeleteOriginal,
                EnabledPlatforms = c.EnabledPlatforms?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet()
                    ?? new HashSet<string>(Platforms.Keys),
            };
        _loaded = true;
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null || msg.Author.IsBot) return false;

        await EnsureLoadedAsync();

        if (!_settings.TryGetValue(guild.Id, out var settings) || !settings.Enabled)
            return false;

        var content = msg.Content;
        if (string.IsNullOrWhiteSpace(content)) return false;

        var fixedLinks = new List<string>();
        var originalUrl = "";

        foreach (var (platform, pattern) in LinkPatterns)
        {
            if (!settings.EnabledPlatforms.Contains(platform))
                continue;

            var matches = pattern.Matches(content);
            if (matches.Count == 0) continue;

            var fixDomain = Platforms[platform].FixDomain;

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                originalUrl = match.Value;
                var fixedUrl = platform switch
                {
                    "twitter" => Regex.Replace(originalUrl, @"(twitter\.com|x\.com)", fixDomain, RegexOptions.IgnoreCase),
                    "instagram" => Regex.Replace(originalUrl, @"instagram\.com", fixDomain, RegexOptions.IgnoreCase),
                    "tiktok" => Regex.Replace(originalUrl, @"tiktok\.com", fixDomain, RegexOptions.IgnoreCase),
                    "reddit" => Regex.Replace(originalUrl, @"reddit\.com", fixDomain, RegexOptions.IgnoreCase),
                    "bluesky" => Regex.Replace(originalUrl, @"bsky\.app", fixDomain, RegexOptions.IgnoreCase),
                    _ => null,
                };

                if (fixedUrl is not null && fixedUrl != originalUrl)
                    fixedLinks.Add(fixedUrl);
            }
        }

        if (fixedLinks.Count == 0) return false;

        // Post the fixed links
        var response = string.Join("\n", fixedLinks);
        await msg.Channel.SendMessageAsync($"📎 **Embed fix** (from {msg.Author.Mention}):\n{response}");

        // Optionally delete the original message
        if (settings.DeleteOriginal)
        {
            try { await msg.DeleteAsync(); }
            catch { /* may lack permissions */ }
        }
        else
        {
            // Suppress embeds on original message so only the fixed one shows
            try { await msg.ModifyAsync(m => m.Flags = MessageFlags.SuppressEmbeds); }
            catch { /* may lack permissions */ }
        }

        return false; // don't block other handlers
    }

    // ── Config Management ──

    public async Task SetEnabledAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<EmbedFixConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
            await ctx.GetTable<EmbedFixConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new EmbedFixConfig { Enabled = enabled });
        else
        {
            ctx.Add(new EmbedFixConfig
            {
                GuildId = guildId,
                Enabled = enabled,
                EnabledPlatforms = string.Join(",", Platforms.Keys),
            });
            await ctx.SaveChangesAsync();
        }

        var settings = _settings.GetOrAdd(guildId, _ => new EmbedFixSettings
        {
            EnabledPlatforms = new HashSet<string>(Platforms.Keys),
        });
        settings.Enabled = enabled;
    }

    public async Task SetDeleteOriginalAsync(ulong guildId, bool delete)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<EmbedFixConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(_ => new EmbedFixConfig { DeleteOriginal = delete });

        if (_settings.TryGetValue(guildId, out var settings))
            settings.DeleteOriginal = delete;
    }

    public async Task TogglePlatformAsync(ulong guildId, string platform)
    {
        if (!Platforms.ContainsKey(platform)) return;

        if (_settings.TryGetValue(guildId, out var settings))
        {
            if (settings.EnabledPlatforms.Contains(platform))
                settings.EnabledPlatforms.Remove(platform);
            else
                settings.EnabledPlatforms.Add(platform);

            await using var ctx = _db.GetDbContext();
            await ctx.GetTable<EmbedFixConfig>()
                .Where(x => x.GuildId == guildId)
                .UpdateAsync(_ => new EmbedFixConfig
                {
                    EnabledPlatforms = string.Join(",", settings.EnabledPlatforms),
                });
        }
    }

    public EmbedFixSettings GetSettings(ulong guildId)
        => _settings.TryGetValue(guildId, out var s) ? s : null;

    public class EmbedFixSettings
    {
        public bool Enabled { get; set; }
        public bool DeleteOriginal { get; set; }
        public HashSet<string> EnabledPlatforms { get; set; } = new();
    }
}
