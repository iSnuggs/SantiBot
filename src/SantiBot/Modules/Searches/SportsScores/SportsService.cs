#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Searches.SportsScores;

public sealed class SportsService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private readonly ShardData _shardData;

    private static readonly Dictionary<string, string> LeagueApiMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nfl"] = "https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard",
        ["nba"] = "https://site.api.espn.com/apis/site/v2/sports/basketball/nba/scoreboard",
        ["epl"] = "https://site.api.espn.com/apis/site/v2/sports/soccer/eng.1/scoreboard",
        ["soccer"] = "https://site.api.espn.com/apis/site/v2/sports/soccer/eng.1/scoreboard",
        ["mlb"] = "https://site.api.espn.com/apis/site/v2/sports/baseball/mlb/scoreboard",
        ["nhl"] = "https://site.api.espn.com/apis/site/v2/sports/hockey/nhl/scoreboard",
    };

    public SportsService(
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
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await PollSportsScores();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error polling sports scores");
            }
        }
    }

    private async Task PollSportsScores()
    {
        List<SportsFollow> follows;
        await using (var uow = _db.GetDbContext())
        {
            follows = await uow.GetTable<SportsFollow>()
                .Where(x => Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
                .ToListAsyncLinqToDB();
        }

        if (follows.Count == 0) return;

        using var httpClient = _http.CreateClient();
        var grouped = follows.GroupBy(x => x.League.ToLower());

        foreach (var group in grouped)
        {
            if (!LeagueApiMap.TryGetValue(group.Key, out var apiUrl))
                continue;

            try
            {
                var resp = await httpClient.GetStringAsync(apiUrl);
                using var doc = JsonDocument.Parse(resp);
                var events = doc.RootElement.GetProperty("events");

                var liveGames = new List<string>();
                foreach (var ev in events.EnumerateArray().Take(10))
                {
                    var name = ev.GetProperty("name").GetString();
                    var status = ev.GetProperty("status").GetProperty("type")
                        .GetProperty("shortDetail").GetString();
                    var competitions = ev.GetProperty("competitions");
                    var comp = competitions.EnumerateArray().FirstOrDefault();

                    var scores = "";
                    if (comp.ValueKind != JsonValueKind.Undefined &&
                        comp.TryGetProperty("competitors", out var competitors))
                    {
                        var teams = competitors.EnumerateArray().ToList();
                        if (teams.Count >= 2)
                        {
                            var t1 = teams[0].GetProperty("team").GetProperty("abbreviation").GetString();
                            var s1 = teams[0].GetProperty("score").GetString();
                            var t2 = teams[1].GetProperty("team").GetProperty("abbreviation").GetString();
                            var s2 = teams[1].GetProperty("score").GetString();
                            scores = $"**{t1}** {s1} - {s2} **{t2}**";
                        }
                    }

                    liveGames.Add($"{scores} ({status})");
                }

                if (liveGames.Count == 0) continue;

                var description = string.Join("\n", liveGames);

                foreach (var follow in group)
                {
                    var guild = _client.GetGuild(follow.GuildId);
                    var ch = guild?.GetTextChannel(follow.ChannelId);
                    if (ch is null) continue;

                    var embed = _sender.CreateEmbed()
                        .WithTitle($"{group.Key.ToUpper()} Scores Update")
                        .WithDescription(description.TrimTo(2048))
                        .WithFooter($"Updated {DateTime.UtcNow:HH:mm} UTC")
                        .WithOkColor();

                    await _sender.Response(ch).Embed(embed).SendAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error polling {League} scores", group.Key);
            }
        }
    }

    public async Task<string> GetScoresAsync(string league)
    {
        league = league.ToLower();
        if (!LeagueApiMap.TryGetValue(league, out var apiUrl))
            return "Unknown league. Supported: NFL, NBA, EPL/Soccer, MLB, NHL";

        using var httpClient = _http.CreateClient();
        var resp = await httpClient.GetStringAsync(apiUrl);
        using var doc = JsonDocument.Parse(resp);
        var events = doc.RootElement.GetProperty("events");

        var lines = new List<string>();
        foreach (var ev in events.EnumerateArray().Take(15))
        {
            var status = ev.GetProperty("status").GetProperty("type")
                .GetProperty("shortDetail").GetString();
            var competitions = ev.GetProperty("competitions");
            var comp = competitions.EnumerateArray().FirstOrDefault();

            if (comp.ValueKind != JsonValueKind.Undefined &&
                comp.TryGetProperty("competitors", out var competitors))
            {
                var teams = competitors.EnumerateArray().ToList();
                if (teams.Count >= 2)
                {
                    var t1 = teams[0].GetProperty("team").GetProperty("abbreviation").GetString();
                    var s1 = teams[0].GetProperty("score").GetString();
                    var t2 = teams[1].GetProperty("team").GetProperty("abbreviation").GetString();
                    var s2 = teams[1].GetProperty("score").GetString();
                    lines.Add($"**{t1}** {s1} - {s2} **{t2}** ({status})");
                }
            }
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "No games found today.";
    }

    public async Task<bool> FollowAsync(ulong guildId, ulong channelId, string league)
    {
        league = league.ToLower();
        if (!LeagueApiMap.ContainsKey(league)) return false;

        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<SportsFollow>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.League == league);
        if (exists) return false;

        await uow.GetTable<SportsFollow>()
            .InsertAsync(() => new SportsFollow
            {
                GuildId = guildId,
                ChannelId = channelId,
                League = league
            });
        return true;
    }

    public async Task<bool> UnfollowAsync(ulong guildId, string league)
    {
        league = league.ToLower();
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<SportsFollow>()
            .Where(x => x.GuildId == guildId && x.League == league)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<SportsFollow>> ListAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<SportsFollow>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
