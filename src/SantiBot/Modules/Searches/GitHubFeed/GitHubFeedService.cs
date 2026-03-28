using System.Net.Http.Json;
using System.Text.Json;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Searches;

public sealed class GitHubFeedService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IHttpClientFactory _httpFactory;
    private readonly NonBlocking.ConcurrentDictionary<string, GitHubRepoWatch> _watches = new();

    private Timer? _pollTimer;

    public GitHubFeedService(DbService db, DiscordSocketClient client, IHttpClientFactory httpFactory)
    {
        _db = db;
        _client = client;
        _httpFactory = httpFactory;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var allWatches = await ctx.GetTable<GitHubRepoWatch>()
            .ToListAsyncLinqToDB();

        foreach (var w in allWatches)
            _watches[$"{w.GuildId}:{w.RepoFullName}"] = w;

        _pollTimer = new Timer(async _ => await PollAllReposAsync(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5));
    }

    private async Task PollAllReposAsync()
    {
        foreach (var watch in _watches.Values.ToList())
        {
            try
            {
                await PollRepoAsync(watch);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error polling GitHub repo {Repo}", watch.RepoFullName);
            }
        }
    }

    private async Task PollRepoAsync(GitHubRepoWatch watch)
    {
        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Add("User-Agent", "SantiBot-Discord");
        http.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        var url = $"https://api.github.com/repos/{watch.RepoFullName}/events?per_page=10";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return;

        var json = await response.Content.ReadAsStringAsync();
        var events = JsonDocument.Parse(json).RootElement;

        if (events.ValueKind != JsonValueKind.Array)
            return;

        var newEvents = new List<JsonElement>();
        foreach (var ev in events.EnumerateArray())
        {
            var eventId = ev.GetProperty("id").GetString() ?? "";
            if (eventId == watch.LastEventId)
                break;
            newEvents.Add(ev);
        }

        if (newEvents.Count == 0)
            return;

        // Update the last event ID
        var latestId = newEvents[0].GetProperty("id").GetString() ?? "";
        watch.LastEventId = latestId;
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<GitHubRepoWatch>()
            .Where(x => x.Id == watch.Id)
            .Set(x => x.LastEventId, latestId)
            .UpdateAsync();

        // Process events in reverse chronological order (oldest first)
        newEvents.Reverse();

        var guild = _client.GetGuild(watch.GuildId);
        var channel = guild?.GetTextChannel(watch.ChannelId);
        if (channel is null)
            return;

        foreach (var ev in newEvents.Take(5)) // Max 5 events per poll to avoid spam
        {
            var eventType = ev.GetProperty("type").GetString() ?? "";
            var actor = ev.GetProperty("actor").GetProperty("login").GetString() ?? "unknown";
            var repo = watch.RepoFullName;

            var (title, description, shouldPost) = eventType switch
            {
                "PushEvent" when watch.WatchCommits => FormatPushEvent(ev, actor, repo),
                "IssuesEvent" when watch.WatchIssues => FormatIssuesEvent(ev, actor, repo),
                "PullRequestEvent" when watch.WatchPRs => FormatPREvent(ev, actor, repo),
                "ReleaseEvent" when watch.WatchReleases => FormatReleaseEvent(ev, actor, repo),
                _ => ("", "", false)
            };

            if (!shouldPost)
                continue;

            var eb = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(eventType switch
                {
                    "PushEvent" => new Color(0x2ecc71),
                    "IssuesEvent" => new Color(0xe74c3c),
                    "PullRequestEvent" => new Color(0x9b59b6),
                    "ReleaseEvent" => new Color(0xf39c12),
                    _ => new Color(0x3498db)
                })
                .WithFooter($"{repo} | {eventType}")
                .WithCurrentTimestamp();

            await channel.SendMessageAsync(embed: eb.Build());
            await Task.Delay(500); // Brief delay to avoid rate limits
        }
    }

    private static (string title, string desc, bool shouldPost) FormatPushEvent(
        JsonElement ev, string actor, string repo)
    {
        var payload = ev.GetProperty("payload");
        var commits = payload.GetProperty("commits");
        var count = commits.GetArrayLength();
        var branch = payload.GetProperty("ref").GetString()?.Replace("refs/heads/", "") ?? "unknown";

        var desc = $"**{actor}** pushed {count} commit(s) to `{branch}`\n";
        foreach (var commit in commits.EnumerateArray().Take(3))
        {
            var msg = commit.GetProperty("message").GetString() ?? "";
            var sha = commit.GetProperty("sha").GetString()?[..7] ?? "";
            if (msg.Length > 80) msg = msg[..80] + "...";
            desc += $"`{sha}` {msg}\n";
        }

        if (count > 3)
            desc += $"... and {count - 3} more";

        return ($"Push to {repo}", desc, true);
    }

    private static (string title, string desc, bool shouldPost) FormatIssuesEvent(
        JsonElement ev, string actor, string repo)
    {
        var payload = ev.GetProperty("payload");
        var action = payload.GetProperty("action").GetString() ?? "";
        var issue = payload.GetProperty("issue");
        var title = issue.GetProperty("title").GetString() ?? "";
        var number = issue.GetProperty("number").GetInt32();
        var url = issue.GetProperty("html_url").GetString() ?? "";

        return ($"Issue #{number} {action}", $"**{actor}** {action} issue [#{number} {title}]({url})", true);
    }

    private static (string title, string desc, bool shouldPost) FormatPREvent(
        JsonElement ev, string actor, string repo)
    {
        var payload = ev.GetProperty("payload");
        var action = payload.GetProperty("action").GetString() ?? "";
        var pr = payload.GetProperty("pull_request");
        var title = pr.GetProperty("title").GetString() ?? "";
        var number = pr.GetProperty("number").GetInt32();
        var url = pr.GetProperty("html_url").GetString() ?? "";

        return ($"PR #{number} {action}", $"**{actor}** {action} pull request [#{number} {title}]({url})", true);
    }

    private static (string title, string desc, bool shouldPost) FormatReleaseEvent(
        JsonElement ev, string actor, string repo)
    {
        var payload = ev.GetProperty("payload");
        var action = payload.GetProperty("action").GetString() ?? "";
        var release = payload.GetProperty("release");
        var tagName = release.GetProperty("tag_name").GetString() ?? "";
        var name = release.GetProperty("name").GetString() ?? tagName;
        var url = release.GetProperty("html_url").GetString() ?? "";

        return ($"Release {tagName}", $"**{actor}** {action} release [{name}]({url})", true);
    }

    public async Task<GitHubRepoWatch> WatchRepoAsync(ulong guildId, ulong channelId, string repoFullName)
    {
        var key = $"{guildId}:{repoFullName}";

        await using var ctx = _db.GetDbContext();

        // Check if already watching
        var existing = await ctx.GetTable<GitHubRepoWatch>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.RepoFullName == repoFullName);

        if (existing is not null)
        {
            // Update channel
            await ctx.GetTable<GitHubRepoWatch>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.ChannelId, channelId)
                .UpdateAsync();
            existing.ChannelId = channelId;
            _watches[key] = existing;
            return existing;
        }

        var watch = new GitHubRepoWatch
        {
            GuildId = guildId,
            ChannelId = channelId,
            RepoFullName = repoFullName
        };

        var id = await ctx.GetTable<GitHubRepoWatch>().InsertWithInt32IdentityAsync(() => new GitHubRepoWatch
        {
            GuildId = guildId,
            ChannelId = channelId,
            RepoFullName = repoFullName,
            LastEventId = "",
            WatchCommits = true,
            WatchIssues = true,
            WatchPRs = true,
            WatchReleases = true
        });

        watch.Id = id;
        _watches[key] = watch;
        return watch;
    }

    public async Task<bool> UnwatchRepoAsync(ulong guildId, string repoFullName)
    {
        var key = $"{guildId}:{repoFullName}";

        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<GitHubRepoWatch>()
            .Where(x => x.GuildId == guildId && x.RepoFullName == repoFullName)
            .DeleteAsync();

        _watches.TryRemove(key, out _);
        return deleted > 0;
    }

    public async Task<List<GitHubRepoWatch>> ListWatchesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<GitHubRepoWatch>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> ToggleEventTypeAsync(ulong guildId, string repoFullName, string eventType, bool enabled)
    {
        var key = $"{guildId}:{repoFullName}";

        await using var ctx = _db.GetDbContext();
        var query = ctx.GetTable<GitHubRepoWatch>()
            .Where(x => x.GuildId == guildId && x.RepoFullName == repoFullName);

        int updated = eventType.ToLower() switch
        {
            "commits" => await query.Set(x => x.WatchCommits, enabled).UpdateAsync(),
            "issues" => await query.Set(x => x.WatchIssues, enabled).UpdateAsync(),
            "prs" => await query.Set(x => x.WatchPRs, enabled).UpdateAsync(),
            "releases" => await query.Set(x => x.WatchReleases, enabled).UpdateAsync(),
            _ => 0
        };

        if (updated > 0 && _watches.TryGetValue(key, out var watch))
        {
            switch (eventType.ToLower())
            {
                case "commits": watch.WatchCommits = enabled; break;
                case "issues": watch.WatchIssues = enabled; break;
                case "prs": watch.WatchPRs = enabled; break;
                case "releases": watch.WatchReleases = enabled; break;
            }
        }

        return updated > 0;
    }
}
