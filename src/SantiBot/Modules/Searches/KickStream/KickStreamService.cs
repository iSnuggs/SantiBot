#nullable disable
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Searches;

/// <summary>
/// Kick.com stream live notifications. Polls the Kick API every 2 minutes
/// and sends a Discord notification when a followed streamer goes live.
/// </summary>
public sealed class KickStreamService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private Timer _timer;

    public KickStreamService(
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
        // Check for live Kick streamers every 2 minutes
        _timer = new Timer(async _ => await CheckAllStreamsAsync(), null,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

        Log.Information("Kick stream checker started (2 minute interval)");
        return Task.CompletedTask;
    }

    private async Task CheckAllStreamsAsync()
    {
        try
        {
            await using var uow = _db.GetDbContext();
            var follows = await uow.Set<KickStreamFollow>().ToListAsyncEF();

            if (follows.Count == 0)
                return;

            var byUser = follows.GroupBy(f => f.KickUsername.ToLowerInvariant());

            foreach (var group in byUser)
            {
                try
                {
                    var username = group.Key;
                    var (isLive, title, thumbnail, viewerCount) = await CheckStreamStatusAsync(username);

                    foreach (var follow in group)
                    {
                        if (isLive && !follow.IsLive)
                        {
                            // Streamer just went live — send notification
                            var guild = _client.GetGuild(follow.GuildId);
                            var channel = guild?.GetTextChannel(follow.NotifyChannelId);

                            if (channel is not null)
                            {
                                var streamUrl = $"https://kick.com/{username}";
                                var customMsg = string.IsNullOrWhiteSpace(follow.CustomMessage)
                                    ? $"**{username}** is now live on Kick!"
                                    : follow.CustomMessage;

                                var embed = _sender.CreateEmbed(follow.GuildId)
                                    .WithTitle($"🟢 {username} is LIVE on Kick!")
                                    .WithUrl(streamUrl)
                                    .WithDescription($"{customMsg}\n\n**{title ?? "No title"}**")
                                    .WithColor(new Discord.Color(0x53FC18)) // Kick green
                                    .WithTimestamp(DateTime.UtcNow);

                                if (!string.IsNullOrEmpty(thumbnail))
                                    embed.WithImageUrl(thumbnail);

                                if (viewerCount >= 0)
                                    embed.AddField("Viewers", viewerCount.ToString("N0"), true);

                                await channel.SendMessageAsync(streamUrl, embed: embed.Build());
                            }
                        }

                        follow.IsLive = isLive;
                    }

                    await uow.SaveChangesAsync();
                    await Task.Delay(1500); // Rate limit friendly
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Kick stream check failed for {Username}", group.Key);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Kick stream checker error");
        }
    }

    /// <summary>
    /// Checks if a Kick streamer is currently live.
    /// </summary>
    private async Task<(bool isLive, string title, string thumbnail, int viewerCount)> CheckStreamStatusAsync(string username)
    {
        try
        {
            using var httpClient = _http.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            var response = await httpClient.GetAsync($"https://kick.com/api/v2/channels/{username}");

            if (!response.IsSuccessStatusCode)
                return (false, null, null, -1);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var livestream = root.TryGetProperty("livestream", out var ls) ? ls : default;

            if (livestream.ValueKind == JsonValueKind.Null || livestream.ValueKind == JsonValueKind.Undefined)
                return (false, null, null, -1);

            var isLive = livestream.TryGetProperty("is_live", out var liveVal) && liveVal.GetBoolean();
            var title = livestream.TryGetProperty("session_title", out var titleVal) ? titleVal.GetString() : null;
            var thumbnail = livestream.TryGetProperty("thumbnail", out var thumbVal)
                ? thumbVal.TryGetProperty("url", out var thumbUrl) ? thumbUrl.GetString() : null
                : null;
            var viewerCount = livestream.TryGetProperty("viewer_count", out var viewerVal) ? viewerVal.GetInt32() : -1;

            return (isLive, title, thumbnail, viewerCount);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check Kick stream status for {Username}", username);
            return (false, null, null, -1);
        }
    }

    // ── Public API ──

    public async Task<(bool success, string error)> FollowAsync(ulong guildId, ulong channelId, string username, string customMessage = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Please provide a Kick username.");

        username = username.Trim().ToLowerInvariant();

        await using var uow = _db.GetDbContext();

        var existing = await uow.Set<KickStreamFollow>()
            .FirstOrDefaultAsyncEF(f => f.GuildId == guildId && f.NotifyChannelId == channelId
                && f.KickUsername.ToLower() == username);

        if (existing is not null)
            return (false, $"This channel is already following **{username}** on Kick.");

        uow.Set<KickStreamFollow>().Add(new KickStreamFollow
        {
            GuildId = guildId,
            NotifyChannelId = channelId,
            KickUsername = username,
            IsLive = false,
            CustomMessage = customMessage ?? "",
        });

        await uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> UnfollowAsync(ulong guildId, string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        username = username.Trim().ToLowerInvariant();

        await using var uow = _db.GetDbContext();
        var follow = await uow.Set<KickStreamFollow>()
            .FirstOrDefaultAsyncEF(f => f.GuildId == guildId
                && f.KickUsername.ToLower() == username);

        if (follow is null)
            return false;

        uow.Set<KickStreamFollow>().Remove(follow);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<List<KickStreamFollow>> GetFollowsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<KickStreamFollow>()
            .AsNoTracking()
            .Where(f => f.GuildId == guildId)
            .ToListAsyncEF();
    }
}
