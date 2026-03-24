#nullable disable
using System.Text.RegularExpressions;
using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Searches;

/// <summary>
/// TikTok feed notifications via RSS bridge services.
/// Uses RSSHub (rsshub.app) or ProxiTok to convert TikTok profiles to RSS.
/// Falls back to the existing Feed system for actual polling.
/// </summary>
public sealed class TikTokFeedService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _httpFactory;

    // RSSHub instance for TikTok RSS conversion
    // Users can self-host RSSHub for reliability
    private const string RSSHUB_BASE = "https://rsshub.app";

    private static readonly Regex _tiktokUserRegex = new(
        @"(?:tiktok\.com/@|^@?)([a-zA-Z0-9_.]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TikTokFeedService(DbService db, DiscordSocketClient client,
        IMessageSenderService sender, IHttpClientFactory httpFactory)
    {
        _db = db;
        _client = client;
        _sender = sender;
        _httpFactory = httpFactory;
    }

    /// <summary>
    /// Resolves a TikTok username or URL to an RSS feed URL.
    /// </summary>
    public string GetRssFeedUrl(string input)
    {
        var match = _tiktokUserRegex.Match(input);
        if (!match.Success)
            return null;

        var username = match.Groups[1].Value;
        // RSSHub route for TikTok user posts
        return $"{RSSHUB_BASE}/tiktok/user/@{username}";
    }

    /// <summary>
    /// Subscribes a channel to a TikTok user's posts via RSS.
    /// Leverages the existing FeedSub system.
    /// </summary>
    public async Task<(bool success, string error, string feedUrl)> SubscribeAsync(
        ulong guildId, ulong channelId, string tiktokUser)
    {
        var feedUrl = GetRssFeedUrl(tiktokUser);
        if (feedUrl is null)
            return (false, "Invalid TikTok username. Use @username or a TikTok profile URL.", null);

        // Test if the feed is reachable
        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            var response = await client.GetAsync(feedUrl);

            if (!response.IsSuccessStatusCode)
                return (false, $"Could not reach TikTok RSS feed. The RSSHub service may be down or rate-limited. (HTTP {(int)response.StatusCode})", null);
        }
        catch (Exception ex)
        {
            return (false, $"Could not reach RSS feed: {ex.Message}", null);
        }

        // Add as a regular feed subscription
        await using var uow = _db.GetDbContext();

        // Check if already subscribed
        var existing = await uow.Set<FeedSub>()
            .FirstOrDefaultAsyncEF(f => f.GuildId == guildId && f.ChannelId == channelId && f.Url == feedUrl);

        if (existing is not null)
            return (false, "This channel is already subscribed to that TikTok user.", null);

        // Check guild feed limit
        var guildCount = await uow.Set<FeedSub>()
            .CountAsyncEF(f => f.GuildId == guildId);

        if (guildCount >= 10) // Max feeds from searches config
            return (false, "Maximum number of feed subscriptions reached.", null);

        var sub = new FeedSub
        {
            GuildId = guildId,
            ChannelId = channelId,
            Url = feedUrl,
            Message = null,
        };

        uow.Set<FeedSub>().Add(sub);
        await uow.SaveChangesAsync();

        return (true, null, feedUrl);
    }

    /// <summary>
    /// Unsubscribes a channel from a TikTok user's posts.
    /// </summary>
    public async Task<bool> UnsubscribeAsync(ulong guildId, ulong channelId, string tiktokUser)
    {
        var feedUrl = GetRssFeedUrl(tiktokUser);
        if (feedUrl is null)
            return false;

        await using var uow = _db.GetDbContext();
        var sub = await uow.Set<FeedSub>()
            .FirstOrDefaultAsyncEF(f => f.GuildId == guildId && f.ChannelId == channelId && f.Url == feedUrl);

        if (sub is null)
            return false;

        uow.Set<FeedSub>().Remove(sub);
        await uow.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Lists all TikTok feed subscriptions for a guild.
    /// </summary>
    public async Task<List<FeedSub>> GetTikTokFeedsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<FeedSub>()
            .AsNoTracking()
            .Where(f => f.GuildId == guildId && f.Url.Contains("tiktok"))
            .ToListAsyncEF();
    }
}
