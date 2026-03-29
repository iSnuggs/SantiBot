#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Xml.Linq;

namespace SantiBot.Modules.Searches.XFeed;

public sealed class XFeedService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private readonly ShardData _shardData;

    // Public nitter instances for RSS
    private static readonly string[] NitterInstances =
    [
        "https://nitter.privacydev.net",
        "https://nitter.poast.org"
    ];

    public XFeedService(
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
                await PollXFeeds();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error polling X/Twitter feeds");
            }
        }
    }

    private async Task PollXFeeds()
    {
        List<XFeedFollow> follows;
        await using (var uow = _db.GetDbContext())
        {
            follows = await uow.GetTable<XFeedFollow>()
                .Where(x => Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
                .ToListAsyncLinqToDB();
        }

        if (follows.Count == 0) return;

        using var httpClient = _http.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SantiBot/1.0");

        var grouped = follows.GroupBy(x => x.Handle.ToLower());
        foreach (var group in grouped)
        {
            try
            {
                var handle = group.Key;
                XDocument doc = null;

                foreach (var instance in NitterInstances)
                {
                    try
                    {
                        var rssUrl = $"{instance}/{handle}/rss";
                        var xml = await httpClient.GetStringAsync(rssUrl);
                        doc = XDocument.Parse(xml);
                        break;
                    }
                    catch { /* try next instance */ }
                }

                if (doc is null) continue;

                var items = doc.Descendants("item").Take(3).ToList();
                if (items.Count == 0) continue;

                foreach (var follow in group)
                {
                    var firstItem = items.First();
                    var itemId = firstItem.Element("guid")?.Value
                                 ?? firstItem.Element("link")?.Value;

                    if (itemId == follow.LastItemId) continue;

                    if (string.IsNullOrEmpty(follow.LastItemId))
                    {
                        await UpdateLastItemId(follow.Id, itemId);
                        continue;
                    }

                    var title = firstItem.Element("title")?.Value ?? "New Post";
                    var link = firstItem.Element("link")?.Value ?? "";
                    var desc = firstItem.Element("description")?.Value?.StripHtml()?.TrimTo(300) ?? "";

                    var guild = _client.GetGuild(follow.GuildId);
                    var ch = guild?.GetTextChannel(follow.ChannelId);
                    if (ch is null) continue;

                    var embed = _sender.CreateEmbed()
                        .WithTitle($"@{handle}: {title.TrimTo(200)}")
                        .WithUrl(link)
                        .WithDescription(desc)
                        .WithFooter($"X/Twitter - @{handle}")
                        .WithOkColor();

                    await _sender.Response(ch).Embed(embed).SendAsync();
                    await UpdateLastItemId(follow.Id, itemId);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error polling X feed for @{Handle}", group.Key);
            }
        }
    }

    private async Task UpdateLastItemId(int id, string itemId)
    {
        await using var uow = _db.GetDbContext();
        await uow.GetTable<XFeedFollow>()
            .Where(x => x.Id == id)
            .UpdateAsync(x => new XFeedFollow { LastItemId = itemId });
    }

    public async Task<bool> FollowAsync(ulong guildId, ulong channelId, string handle)
    {
        handle = handle.ToLower().Trim().TrimStart('@');
        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<XFeedFollow>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.Handle == handle);
        if (exists) return false;

        await uow.GetTable<XFeedFollow>()
            .InsertAsync(() => new XFeedFollow
            {
                GuildId = guildId,
                ChannelId = channelId,
                Handle = handle
            });
        return true;
    }

    public async Task<bool> UnfollowAsync(ulong guildId, string handle)
    {
        handle = handle.ToLower().Trim().TrimStart('@');
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<XFeedFollow>()
            .Where(x => x.GuildId == guildId && x.Handle == handle)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<XFeedFollow>> ListAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<XFeedFollow>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
