using System.Text.RegularExpressions;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class PhishingDetectService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    // Known phishing domain patterns (compiled for performance)
    private static readonly Regex[] _phishingPatterns =
    [
        // Fake Discord links
        new(@"discord[\-\.]?nitro[\-\.]?free", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"discord[\-\.]?gift[\-\.]?free", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"dlscord[\.\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"discorcl[\.\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"disc0rd[\.\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Fake Steam links
        new(@"stearn(community|powered)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"steamcommunlty", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"steamcommunity[\-\.]giveaway", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"store\.stearnpowered", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // General phishing
        new(@"free[\-\.]?nitro[\-\.]?(gift|generator|claim)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(claim|get)[\-\.]?your[\-\.]?nitro", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"airdrop[\-\.]?(claim|free|gift)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Homograph attacks: common substitutions
        new(@"discordapp\.(co|gift|click|top|gg\.com)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    // Suspicious TLDs commonly used by phishing sites
    private static readonly HashSet<string> _suspiciousTlds = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xyz", ".top", ".click", ".icu", ".buzz", ".gq", ".ml", ".tk", ".cf", ".ga",
        ".cam", ".rest", ".monster", ".surf", ".quest"
    };

    // URL extraction pattern
    private static readonly Regex _urlPattern = new(
        @"https?://[^\s<>\[\]""']+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PhishingDetectService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessageReceived;
        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(SocketMessage msg)
    {
        if (msg is not SocketUserMessage userMsg)
            return;
        if (msg.Author.IsBot)
            return;
        if (msg.Channel is not SocketTextChannel textCh)
            return;

        try
        {
            var config = await GetConfigAsync(textCh.Guild.Id);
            if (config is null || !config.Enabled)
                return;

            var result = CheckMessageForPhishing(msg.Content);
            if (result is null)
                return;

            await TakeActionAsync(userMsg, textCh.Guild, config, result);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in PhishingDetect for message {MsgId}", msg.Id);
        }
    }

    /// <summary>
    /// Check message content for phishing. Returns the matched pattern or null.
    /// </summary>
    public string? CheckMessageForPhishing(string content)
    {
        // Extract URLs
        var urls = _urlPattern.Matches(content);
        foreach (Match urlMatch in urls)
        {
            var url = urlMatch.Value.ToLowerInvariant();

            // Check against known phishing patterns
            foreach (var pattern in _phishingPatterns)
            {
                if (pattern.IsMatch(url))
                    return $"Matched phishing pattern: `{pattern}`";
            }

            // Check suspicious TLDs
            foreach (var tld in _suspiciousTlds)
            {
                if (url.Contains(tld + "/") || url.EndsWith(tld))
                {
                    // Only flag suspicious TLDs that also contain bait keywords
                    if (Regex.IsMatch(url, @"(discord|nitro|steam|free|gift|airdrop)", RegexOptions.IgnoreCase))
                        return $"Suspicious TLD with bait keyword: `{url}`";
                }
            }
        }

        return null;
    }

    private async Task TakeActionAsync(SocketUserMessage msg, SocketGuild guild, PhishingConfig config, string reason)
    {
        var action = config.Action?.ToLowerInvariant() ?? "delete";
        var user = msg.Author as SocketGuildUser;

        switch (action)
        {
            case "delete":
                try { await msg.DeleteAsync(); }
                catch { /* Missing permissions */ }
                break;

            case "warn":
                try { await msg.DeleteAsync(); }
                catch { /* Missing permissions */ }
                // Warn is logged below; a full warn system would tie into ModCase
                break;

            case "mute":
                try { await msg.DeleteAsync(); }
                catch { /* Missing permissions */ }
                if (user is not null)
                {
                    try { await user.SetTimeOutAsync(TimeSpan.FromMinutes(10), new RequestOptions { AuditLogReason = "Phishing link detected" }); }
                    catch { /* Missing permissions */ }
                }
                break;

            case "ban":
                try { await msg.DeleteAsync(); }
                catch { /* Missing permissions */ }
                if (user is not null)
                {
                    try { await guild.AddBanAsync(user, reason: "Phishing link detected"); }
                    catch { /* Missing permissions */ }
                }
                break;
        }

        // Log to configured channel
        if (config.LogChannelId is { } logChId && guild.GetTextChannel(logChId) is { } logCh)
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Phishing Link Detected")
                .WithDescription($"**User:** {msg.Author.Mention} ({msg.Author.Username})\n**Channel:** <#{msg.Channel.Id}>\n**Action:** {action}")
                .AddField("Reason", reason)
                .AddField("Message Content", msg.Content.Length > 1000 ? msg.Content[..1000] + "..." : msg.Content)
                .WithCurrentTimestamp()
                .Build();

            try { await logCh.SendMessageAsync(embed: embed); }
            catch { /* Missing permissions */ }
        }
    }

    // ── Config CRUD ──

    public async Task<PhishingConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PhishingConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<PhishingConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<PhishingConfig>()
                .InsertAsync(() => new PhishingConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                });
        }
        else
        {
            await ctx.GetTable<PhishingConfig>()
                .Where(x => x.GuildId == guildId)
                .UpdateAsync(x => new PhishingConfig { Enabled = enabled });
        }
    }

    public async Task SetActionAsync(ulong guildId, string action)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<PhishingConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new PhishingConfig { Action = action });
    }

    public async Task SetLogChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<PhishingConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new PhishingConfig { LogChannelId = channelId });
    }

    private async Task EnsureConfigAsync(SantiContext ctx, ulong guildId)
    {
        var exists = await ctx.GetTable<PhishingConfig>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId);

        if (!exists)
        {
            await ctx.GetTable<PhishingConfig>()
                .InsertAsync(() => new PhishingConfig
                {
                    GuildId = guildId,
                    Enabled = false,
                });
        }
    }
}
