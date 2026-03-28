#nullable disable
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Searches;

/// <summary>
/// YouTube feed notifications by polling YouTube's public RSS feeds.
/// No API key required — uses the built-in Atom feeds.
/// </summary>
public sealed class YouTubeFeedService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private Timer _timer;

    public YouTubeFeedService(
        DbService db,
        DiscordSocketClient client,
        IMessageSenderService sender,
        IHttpClientFactory http)
    {
        _db = db;
        _client = client;
        _sender = sender;
        _http = http;
    }

    public Task OnReadyAsync()
    {
        // Check for new YouTube videos every 10 minutes
        _timer = new Timer(async _ => await CheckAllFeedsAsync(), null,
            TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));

        Log.Information("YouTube feed checker started (10 minute interval)");
        return Task.CompletedTask;
    }

    private async Task CheckAllFeedsAsync()
    {
        try
        {
            await using var uow = _db.GetDbContext();
            var subs = await uow.Set<YouTubeFeedSub>().ToListAsyncEF();

            if (subs.Count == 0)
                return;

            // Group by YouTube channel to avoid duplicate fetches
            var byChannel = subs.GroupBy(s => s.YouTubeChannelId);

            foreach (var group in byChannel)
            {
                try
                {
                    var ytChannelId = group.Key;
                    var (videoId, videoTitle, videoUrl, channelName) = await FetchLatestVideoAsync(ytChannelId);

                    if (string.IsNullOrEmpty(videoId))
                        continue;

                    foreach (var sub in group)
                    {
                        // Update channel name if we got a better one
                        if (!string.IsNullOrEmpty(channelName) && sub.YouTubeChannelName != channelName)
                            sub.YouTubeChannelName = channelName;

                        sub.LastChecked = DateTime.UtcNow;

                        // Skip if already notified
                        if (sub.LastVideoId == videoId)
                            continue;

                        // First-time sub — save ID without notifying
                        if (string.IsNullOrEmpty(sub.LastVideoId))
                        {
                            sub.LastVideoId = videoId;
                            continue;
                        }

                        // New video — send notification
                        var guild = _client.GetGuild(sub.GuildId);
                        var channel = guild?.GetTextChannel(sub.ChannelId);

                        if (channel is not null)
                        {
                            var embed = _sender.CreateEmbed(sub.GuildId)
                                .WithTitle($"📺 New Video from {channelName ?? ytChannelId}")
                                .WithUrl(videoUrl)
                                .WithDescription($"**{videoTitle}**")
                                .WithColor(new Discord.Color(0xFF0000)) // YouTube red
                                .WithImageUrl($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg")
                                .WithTimestamp(DateTime.UtcNow);

                            await channel.SendMessageAsync(videoUrl, embed: embed.Build());
                        }

                        sub.LastVideoId = videoId;
                    }

                    await uow.SaveChangesAsync();
                    await Task.Delay(2000); // Rate limit friendly
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "YouTube feed check failed for channel {ChannelId}", group.Key);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "YouTube feed checker error");
        }
    }

    /// <summary>
    /// Fetches the latest video from a YouTube channel's RSS feed.
    /// </summary>
    private async Task<(string videoId, string title, string url, string channelName)> FetchLatestVideoAsync(string ytChannelId)
    {
        try
        {
            using var httpClient = _http.CreateClient();
            var feedUrl = $"https://www.youtube.com/feeds/videos.xml?channel_id={ytChannelId}";
            var xml = await httpClient.GetStringAsync(feedUrl);

            var doc = XDocument.Parse(xml);
            XNamespace atom = "http://www.w3.org/2005/Atom";
            XNamespace yt = "urn:youtube:yt";

            var channelName = doc.Root?.Element(atom + "title")?.Value;
            var entry = doc.Root?.Element(atom + "entry");

            if (entry is null)
                return (null, null, null, channelName);

            var videoId = entry.Element(yt + "videoId")?.Value;
            var title = entry.Element(atom + "title")?.Value;
            var link = entry.Element(atom + "link")?.Attribute("href")?.Value;

            return (videoId, title, link ?? $"https://www.youtube.com/watch?v={videoId}", channelName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to fetch YouTube RSS feed for {ChannelId}", ytChannelId);
            return (null, null, null, null);
        }
    }

    /// <summary>
    /// Validates a YouTube channel ID by checking if the RSS feed is reachable.
    /// Also returns the channel name if found.
    /// </summary>
    private async Task<(bool valid, string channelName)> ValidateChannelAsync(string ytChannelId)
    {
        var (videoId, _, _, channelName) = await FetchLatestVideoAsync(ytChannelId);
        // Even if there are no videos, if we got a channel name the feed is valid
        return (!string.IsNullOrEmpty(videoId) || !string.IsNullOrEmpty(channelName), channelName);
    }

    // ── Public API ──

    public async Task<(bool success, string error)> FollowAsync(ulong guildId, ulong channelId, string ytChannelId)
    {
        if (string.IsNullOrWhiteSpace(ytChannelId))
            return (false, "Please provide a YouTube channel ID (starts with UC).");

        ytChannelId = ytChannelId.Trim();

        // Validate the channel
        var (valid, channelName) = await ValidateChannelAsync(ytChannelId);
        if (!valid)
            return (false, $"Could not find YouTube channel `{ytChannelId}`. Make sure you're using the channel ID (starts with UC).");

        await using var uow = _db.GetDbContext();

        var existing = await uow.Set<YouTubeFeedSub>()
            .FirstOrDefaultAsyncEF(s => s.GuildId == guildId && s.ChannelId == channelId
                && s.YouTubeChannelId == ytChannelId);

        if (existing is not null)
            return (false, $"This channel is already following **{channelName ?? ytChannelId}**.");

        var (latestId, _, _, _) = await FetchLatestVideoAsync(ytChannelId);

        uow.Set<YouTubeFeedSub>().Add(new YouTubeFeedSub
        {
            GuildId = guildId,
            ChannelId = channelId,
            YouTubeChannelId = ytChannelId,
            YouTubeChannelName = channelName ?? ytChannelId,
            LastVideoId = latestId ?? "",
            LastChecked = DateTime.UtcNow,
        });

        await uow.SaveChangesAsync();
        return (true, channelName ?? ytChannelId);
    }

    public async Task<bool> UnfollowAsync(ulong guildId, string ytChannelId)
    {
        if (string.IsNullOrWhiteSpace(ytChannelId))
            return false;

        await using var uow = _db.GetDbContext();
        var sub = await uow.Set<YouTubeFeedSub>()
            .FirstOrDefaultAsyncEF(s => s.GuildId == guildId
                && s.YouTubeChannelId == ytChannelId.Trim());

        if (sub is null)
            return false;

        uow.Set<YouTubeFeedSub>().Remove(sub);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<List<YouTubeFeedSub>> GetSubsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<YouTubeFeedSub>()
            .AsNoTracking()
            .Where(s => s.GuildId == guildId)
            .ToListAsyncEF();
    }
}
