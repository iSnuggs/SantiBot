#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Searches.AnimeTracker;

public sealed class AnimeTrackerService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private readonly ShardData _shardData;

    private const string ANILIST_API = "https://graphql.anilist.co";

    public AnimeTrackerService(
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
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await PollAnimeEpisodes();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error polling anime episodes");
            }
        }
    }

    private async Task PollAnimeEpisodes()
    {
        List<AnimeTrack> tracks;
        await using (var uow = _db.GetDbContext())
        {
            tracks = await uow.GetTable<AnimeTrack>()
                .Where(x => Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
                .ToListAsyncLinqToDB();
        }

        if (tracks.Count == 0) return;

        using var httpClient = _http.CreateClient();
        var grouped = tracks.GroupBy(x => x.AniListId);

        foreach (var group in grouped)
        {
            try
            {
                var aniId = group.Key;
                var query = @"{
                    Media(id: " + aniId + @", type: ANIME) {
                        id
                        title { romaji english }
                        episodes
                        nextAiringEpisode { episode airingAt }
                        coverImage { large }
                        siteUrl
                        status
                    }
                }";

                var content = new StringContent(
                    JsonSerializer.Serialize(new { query }),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var resp = await httpClient.PostAsync(ANILIST_API, content);
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var media = doc.RootElement.GetProperty("data").GetProperty("Media");

                var nextEp = media.TryGetProperty("nextAiringEpisode", out var nae) && nae.ValueKind != JsonValueKind.Null
                    ? nae.GetProperty("episode").GetInt32()
                    : 0;
                var currentEp = nextEp > 0 ? nextEp - 1 : 0;
                var titleObj = media.GetProperty("title");
                var title = titleObj.TryGetProperty("english", out var en) && en.ValueKind != JsonValueKind.Null
                    ? en.GetString()
                    : titleObj.GetProperty("romaji").GetString();
                var coverUrl = media.TryGetProperty("coverImage", out var ci)
                    ? ci.GetProperty("large").GetString()
                    : null;
                var siteUrl = media.TryGetProperty("siteUrl", out var su) ? su.GetString() : "";

                foreach (var track in group)
                {
                    if (currentEp > track.LastEpisode && track.LastEpisode > 0)
                    {
                        var guild = _client.GetGuild(track.GuildId);
                        var ch = guild?.GetTextChannel(track.ChannelId);
                        if (ch is not null)
                        {
                            var embed = _sender.CreateEmbed()
                                .WithTitle($"{title} - Episode {currentEp}")
                                .WithUrl(siteUrl)
                                .WithDescription($"Episode **{currentEp}** of **{title}** has aired!")
                                .WithOkColor();

                            if (!string.IsNullOrEmpty(coverUrl))
                                embed.WithThumbnailUrl(coverUrl);

                            await _sender.Response(ch).Embed(embed).SendAsync();
                        }
                    }

                    if (currentEp != track.LastEpisode)
                    {
                        await using var uow = _db.GetDbContext();
                        await uow.GetTable<AnimeTrack>()
                            .Where(x => x.Id == track.Id)
                            .UpdateAsync(x => new AnimeTrack { LastEpisode = currentEp });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error polling anime {AniId}", group.Key);
            }
        }
    }

    public async Task<(bool success, int aniId, string title)> TrackAsync(ulong guildId, ulong channelId, string titleSearch)
    {
        // Search AniList for the anime
        using var httpClient = _http.CreateClient();
        var query = @"{
            Media(search: """ + titleSearch.Replace("\"", "\\\"") + @""", type: ANIME) {
                id
                title { romaji english }
                episodes
                nextAiringEpisode { episode }
            }
        }";

        var content = new StringContent(
            JsonSerializer.Serialize(new { query }),
            System.Text.Encoding.UTF8,
            "application/json");

        var resp = await httpClient.PostAsync(ANILIST_API, content);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("data", out var data))
            return (false, 0, null);

        var media = data.GetProperty("Media");
        var aniId = media.GetProperty("id").GetInt32();
        var titleObj = media.GetProperty("title");
        var title = titleObj.TryGetProperty("english", out var en) && en.ValueKind != JsonValueKind.Null
            ? en.GetString()
            : titleObj.GetProperty("romaji").GetString();

        var nextEp = media.TryGetProperty("nextAiringEpisode", out var nae) && nae.ValueKind != JsonValueKind.Null
            ? nae.GetProperty("episode").GetInt32()
            : 0;
        var currentEp = nextEp > 0 ? nextEp - 1 : 0;

        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<AnimeTrack>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.AniListId == aniId);
        if (exists) return (false, aniId, title);

        await uow.GetTable<AnimeTrack>()
            .InsertAsync(() => new AnimeTrack
            {
                GuildId = guildId,
                ChannelId = channelId,
                AniListId = aniId,
                Title = title,
                LastEpisode = currentEp
            });

        return (true, aniId, title);
    }

    public async Task<bool> UntrackAsync(ulong guildId, string titleSearch)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<AnimeTrack>()
            .Where(x => x.GuildId == guildId && x.Title.ToLower().Contains(titleSearch.ToLower()))
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<AnimeTrack>> ListAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<AnimeTrack>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<string> GetScheduleAsync()
    {
        using var httpClient = _http.CreateClient();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var weekLater = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
        var query = @"{
            Page(page: 1, perPage: 15) {
                airingSchedules(airingAt_greater: " + now + @", airingAt_lesser: " + weekLater + @", sort: TIME) {
                    episode
                    airingAt
                    media { title { romaji english } }
                }
            }
        }";

        var content = new StringContent(
            JsonSerializer.Serialize(new { query }),
            System.Text.Encoding.UTF8,
            "application/json");

        var resp = await httpClient.PostAsync(ANILIST_API, content);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var schedules = doc.RootElement
            .GetProperty("data")
            .GetProperty("Page")
            .GetProperty("airingSchedules");

        var lines = new List<string>();
        foreach (var s in schedules.EnumerateArray())
        {
            var ep = s.GetProperty("episode").GetInt32();
            var airingAt = DateTimeOffset.FromUnixTimeSeconds(s.GetProperty("airingAt").GetInt64());
            var titleObj = s.GetProperty("media").GetProperty("title");
            var title = titleObj.TryGetProperty("english", out var en) && en.ValueKind != JsonValueKind.Null
                ? en.GetString()
                : titleObj.GetProperty("romaji").GetString();
            lines.Add($"**{title}** Ep {ep} - <t:{airingAt.ToUnixTimeSeconds()}:R>");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "No upcoming episodes found.";
    }
}
