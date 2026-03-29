#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Searches.RedditFeed;

public sealed class RedditFeedService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private readonly ShardData _shardData;

    public RedditFeedService(
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
                await PollRedditFeeds();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error polling Reddit feeds");
            }
        }
    }

    private async Task PollRedditFeeds()
    {
        List<RedditFollow> follows;
        await using (var uow = _db.GetDbContext())
        {
            follows = await uow.GetTable<RedditFollow>()
                .Where(x => Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
                .ToListAsyncLinqToDB();
        }

        var grouped = follows.GroupBy(x => x.Subreddit.ToLower());
        using var httpClient = _http.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SantiBot/1.0");

        foreach (var group in grouped)
        {
            try
            {
                var sub = group.Key;
                var url = $"https://www.reddit.com/r/{sub}/new.json?limit=5";
                var resp = await httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(resp);
                var children = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("children");

                foreach (var follow in group)
                {
                    foreach (var child in children.EnumerateArray())
                    {
                        var data = child.GetProperty("data");
                        var postId = data.GetProperty("name").GetString();

                        if (postId == follow.LastPostId)
                            break;

                        if (string.IsNullOrEmpty(follow.LastPostId))
                        {
                            // First run, just store the latest
                            await UpdateLastPostId(follow.Id, postId);
                            break;
                        }

                        var title = data.GetProperty("title").GetString();
                        var permalink = data.GetProperty("permalink").GetString();
                        var score = data.GetProperty("score").GetInt32();
                        var thumbnail = data.TryGetProperty("thumbnail", out var thumb)
                            ? thumb.GetString()
                            : null;
                        var author = data.GetProperty("author").GetString();

                        var guild = _client.GetGuild(follow.GuildId);
                        var ch = guild?.GetTextChannel(follow.ChannelId);
                        if (ch is null) continue;

                        var embed = _sender.CreateEmbed()
                            .WithTitle(title?.TrimTo(256) ?? "New Post")
                            .WithUrl($"https://reddit.com{permalink}")
                            .WithDescription($"**r/{sub}** | Score: {score} | by u/{author}")
                            .WithFooter($"r/{sub}")
                            .WithOkColor();

                        if (!string.IsNullOrEmpty(thumbnail) &&
                            Uri.IsWellFormedUriString(thumbnail, UriKind.Absolute) &&
                            thumbnail.StartsWith("http"))
                            embed.WithThumbnailUrl(thumbnail);

                        await _sender.Response(ch).Embed(embed).SendAsync();
                        await UpdateLastPostId(follow.Id, postId);
                        break; // only post newest per cycle
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error polling r/{Subreddit}", group.Key);
            }
        }
    }

    private async Task UpdateLastPostId(int followId, string postId)
    {
        await using var uow = _db.GetDbContext();
        await uow.GetTable<RedditFollow>()
            .Where(x => x.Id == followId)
            .UpdateAsync(x => new RedditFollow { LastPostId = postId });
    }

    public async Task<bool> FollowAsync(ulong guildId, ulong channelId, string subreddit)
    {
        subreddit = subreddit.ToLower().Trim().TrimStart('/').Replace("r/", "");
        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<RedditFollow>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.Subreddit == subreddit);
        if (exists) return false;

        await uow.GetTable<RedditFollow>()
            .InsertAsync(() => new RedditFollow
            {
                GuildId = guildId,
                ChannelId = channelId,
                Subreddit = subreddit
            });
        return true;
    }

    public async Task<bool> UnfollowAsync(ulong guildId, string subreddit)
    {
        subreddit = subreddit.ToLower().Trim().TrimStart('/').Replace("r/", "");
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<RedditFollow>()
            .Where(x => x.GuildId == guildId && x.Subreddit == subreddit)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<RedditFollow>> ListAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<RedditFollow>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
