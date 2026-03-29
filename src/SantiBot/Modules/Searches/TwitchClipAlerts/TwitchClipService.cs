#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Searches.TwitchClipAlerts;

public sealed class TwitchClipService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private readonly ShardData _shardData;

    public TwitchClipService(
        DbService db,
        DiscordSocketClient client,
        IMessageSenderService sender,
        IHttpClientFactory http,
        ShardData shardData)
    {
        _db = db;
        _client = client;
        _sender = sender;
        _http = http;
        _shardData = shardData;
    }

    public async Task OnReadyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await PollClips();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error polling Twitch clips");
            }
        }
    }

    private async Task PollClips()
    {
        List<TwitchClipFollow> follows;
        await using (var uow = _db.GetDbContext())
        {
            follows = await uow.GetTable<TwitchClipFollow>()
                .Where(x => Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
                .ToListAsyncLinqToDB();
        }

        if (follows.Count == 0) return;

        // Use Twitch clips RSS feed or scrape approach (no auth needed)
        using var httpClient = _http.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SantiBot/1.0");

        var grouped = follows.GroupBy(x => x.TwitchChannel.ToLower());
        foreach (var group in grouped)
        {
            try
            {
                var channel = group.Key;
                // Use the Twitch clips discovery page RSS or JSON
                var url = $"https://twitchtracker.com/api/channels/clips/{channel}";
                string resp;
                try
                {
                    resp = await httpClient.GetStringAsync(url);
                }
                catch
                {
                    // Fallback: just skip this cycle
                    continue;
                }

                // Try parsing as JSON array of clips
                using var doc = JsonDocument.Parse(resp);
                var clips = doc.RootElement.EnumerateArray().Take(3).ToList();
                if (clips.Count == 0) continue;

                foreach (var follow in group)
                {
                    var firstClip = clips.First();
                    var clipId = firstClip.TryGetProperty("slug", out var slugProp)
                        ? slugProp.GetString()
                        : firstClip.TryGetProperty("id", out var idProp)
                            ? idProp.GetString()
                            : null;

                    if (clipId is null || clipId == follow.LastClipId) continue;

                    if (string.IsNullOrEmpty(follow.LastClipId))
                    {
                        await UpdateLastClipId(follow.Id, clipId);
                        continue;
                    }

                    var title = firstClip.TryGetProperty("title", out var tp) ? tp.GetString() : "New Clip";
                    var clipUrl = $"https://clips.twitch.tv/{clipId}";

                    var guild = _client.GetGuild(follow.GuildId);
                    var ch = guild?.GetTextChannel(follow.ChannelId);
                    if (ch is null) continue;

                    var embed = _sender.CreateEmbed()
                        .WithTitle($"New Clip from {channel}")
                        .WithDescription(title?.TrimTo(256) ?? "New clip!")
                        .WithUrl(clipUrl)
                        .WithFooter($"Twitch - {channel}")
                        .WithOkColor();

                    await _sender.Response(ch).Embed(embed).SendAsync();
                    await UpdateLastClipId(follow.Id, clipId);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error polling Twitch clips for {Channel}", group.Key);
            }
        }
    }

    private async Task UpdateLastClipId(int id, string clipId)
    {
        await using var uow = _db.GetDbContext();
        await uow.GetTable<TwitchClipFollow>()
            .Where(x => x.Id == id)
            .UpdateAsync(x => new TwitchClipFollow { LastClipId = clipId });
    }

    public async Task<bool> FollowAsync(ulong guildId, ulong channelId, string twitchChannel)
    {
        twitchChannel = twitchChannel.ToLower().Trim();
        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<TwitchClipFollow>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.TwitchChannel == twitchChannel);
        if (exists) return false;

        await uow.GetTable<TwitchClipFollow>()
            .InsertAsync(() => new TwitchClipFollow
            {
                GuildId = guildId,
                ChannelId = channelId,
                TwitchChannel = twitchChannel
            });
        return true;
    }

    public async Task<bool> UnfollowAsync(ulong guildId, string twitchChannel)
    {
        twitchChannel = twitchChannel.ToLower().Trim();
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<TwitchClipFollow>()
            .Where(x => x.GuildId == guildId && x.TwitchChannel == twitchChannel)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<TwitchClipFollow>> ListAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<TwitchClipFollow>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
