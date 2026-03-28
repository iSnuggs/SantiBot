#nullable disable
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Searches;

/// <summary>
/// Bluesky feed notifications. Polls the public Bluesky API every 5 minutes
/// for new posts from followed accounts. No authentication required.
/// </summary>
public sealed class BlueskyFeedService : IReadyExecutor, INService
{
    private const string BSKY_API = "https://public.api.bsky.app/xrpc";

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private Timer _timer;

    public BlueskyFeedService(
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
        // Check for new Bluesky posts every 5 minutes
        _timer = new Timer(async _ => await CheckAllFeedsAsync(), null,
            TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5));

        Log.Information("Bluesky feed checker started (5 minute interval)");
        return Task.CompletedTask;
    }

    private async Task CheckAllFeedsAsync()
    {
        try
        {
            await using var uow = _db.GetDbContext();
            var subs = await uow.Set<BlueskyFeedSub>().ToListAsyncEF();

            if (subs.Count == 0)
                return;

            var byHandle = subs.GroupBy(s => s.BlueskyHandle.ToLowerInvariant());

            foreach (var group in byHandle)
            {
                try
                {
                    var handle = group.Key;
                    var (postUri, postText, postUrl, displayName, avatar) = await FetchLatestPostAsync(handle);

                    if (string.IsNullOrEmpty(postUri))
                        continue;

                    foreach (var sub in group)
                    {
                        sub.LastChecked = DateTime.UtcNow;

                        if (sub.LastPostUri == postUri)
                            continue;

                        // First-time sub — save URI without notifying
                        if (string.IsNullOrEmpty(sub.LastPostUri))
                        {
                            sub.LastPostUri = postUri;
                            continue;
                        }

                        // New post — send notification
                        var guild = _client.GetGuild(sub.GuildId);
                        var channel = guild?.GetTextChannel(sub.ChannelId);

                        if (channel is not null)
                        {
                            var embed = _sender.CreateEmbed(sub.GuildId)
                                .WithTitle($"🦋 New post from {displayName ?? handle}")
                                .WithUrl(postUrl)
                                .WithDescription(postText?.Length > 2048 ? postText[..2045] + "..." : postText ?? "")
                                .WithColor(new Discord.Color(0x0085FF)) // Bluesky blue
                                .WithTimestamp(DateTime.UtcNow);

                            if (!string.IsNullOrEmpty(avatar))
                                embed.WithThumbnailUrl(avatar);

                            await channel.SendMessageAsync(embed: embed.Build());
                        }

                        sub.LastPostUri = postUri;
                    }

                    await uow.SaveChangesAsync();
                    await Task.Delay(2000); // Rate limit friendly
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Bluesky feed check failed for {Handle}", group.Key);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Bluesky feed checker error");
        }
    }

    /// <summary>
    /// Fetches the latest post from a Bluesky user's public feed.
    /// </summary>
    private async Task<(string uri, string text, string webUrl, string displayName, string avatar)> FetchLatestPostAsync(string handle)
    {
        try
        {
            using var httpClient = _http.CreateClient();
            var url = $"{BSKY_API}/app.bsky.feed.getAuthorFeed?actor={Uri.EscapeDataString(handle)}&limit=1&filter=posts_no_replies";
            var json = await httpClient.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            var feed = doc.RootElement.GetProperty("feed");

            if (feed.GetArrayLength() == 0)
                return (null, null, null, null, null);

            var firstPost = feed[0].GetProperty("post");
            var uri = firstPost.GetProperty("uri").GetString();
            var record = firstPost.GetProperty("record");
            var text = record.TryGetProperty("text", out var textVal) ? textVal.GetString() : null;

            var author = firstPost.GetProperty("author");
            var displayName = author.TryGetProperty("displayName", out var dnVal) ? dnVal.GetString() : null;
            var avatar = author.TryGetProperty("avatar", out var avVal) ? avVal.GetString() : null;
            var did = author.GetProperty("did").GetString();

            // Build a web URL from the AT URI: at://did/app.bsky.feed.post/rkey -> https://bsky.app/profile/handle/post/rkey
            var rkey = uri?.Split('/').LastOrDefault();
            var webUrl = $"https://bsky.app/profile/{handle}/post/{rkey}";

            return (uri, text, webUrl, displayName, avatar);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to fetch Bluesky feed for {Handle}", handle);
            return (null, null, null, null, null);
        }
    }

    /// <summary>
    /// Validates a Bluesky handle by attempting to resolve it.
    /// </summary>
    private async Task<(bool valid, string displayName)> ValidateHandleAsync(string handle)
    {
        try
        {
            using var httpClient = _http.CreateClient();
            var url = $"{BSKY_API}/app.bsky.actor.getProfile?actor={Uri.EscapeDataString(handle)}";
            var json = await httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var displayName = doc.RootElement.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
            return (true, displayName);
        }
        catch
        {
            return (false, null);
        }
    }

    // ── Public API ──

    public async Task<(bool success, string error)> FollowAsync(ulong guildId, ulong channelId, string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return (false, "Please provide a Bluesky handle (e.g. user.bsky.social).");

        handle = handle.Trim().TrimStart('@').ToLowerInvariant();

        var (valid, displayName) = await ValidateHandleAsync(handle);
        if (!valid)
            return (false, $"Could not find Bluesky user `{handle}`. Check the spelling.");

        await using var uow = _db.GetDbContext();

        var existing = await uow.Set<BlueskyFeedSub>()
            .FirstOrDefaultAsyncEF(s => s.GuildId == guildId && s.ChannelId == channelId
                && s.BlueskyHandle.ToLower() == handle);

        if (existing is not null)
            return (false, $"This channel is already following **{displayName ?? handle}** on Bluesky.");

        uow.Set<BlueskyFeedSub>().Add(new BlueskyFeedSub
        {
            GuildId = guildId,
            ChannelId = channelId,
            BlueskyHandle = handle,
            LastPostUri = "",
            LastChecked = DateTime.UtcNow,
        });

        await uow.SaveChangesAsync();
        return (true, displayName ?? handle);
    }

    public async Task<bool> UnfollowAsync(ulong guildId, string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return false;

        handle = handle.Trim().TrimStart('@').ToLowerInvariant();

        await using var uow = _db.GetDbContext();
        var sub = await uow.Set<BlueskyFeedSub>()
            .FirstOrDefaultAsyncEF(s => s.GuildId == guildId
                && s.BlueskyHandle.ToLower() == handle);

        if (sub is null)
            return false;

        uow.Set<BlueskyFeedSub>().Remove(sub);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<List<BlueskyFeedSub>> GetSubsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<BlueskyFeedSub>()
            .AsNoTracking()
            .Where(s => s.GuildId == guildId)
            .ToListAsyncEF();
    }
}
