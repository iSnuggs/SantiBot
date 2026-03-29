#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Xml.Linq;

namespace SantiBot.Modules.Searches.RssManager;

public sealed class RssManagerService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private readonly ShardData _shardData;

    public RssManagerService(
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
                await PollFeeds();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error polling RSS feeds");
            }
        }
    }

    private async Task PollFeeds()
    {
        List<RssFeedEntry> feeds;
        await using (var uow = _db.GetDbContext())
        {
            feeds = await uow.GetTable<RssFeedEntry>()
                .Where(x => Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
                .ToListAsyncLinqToDB();
        }

        if (feeds.Count == 0) return;

        using var httpClient = _http.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SantiBot/1.0");

        foreach (var feed in feeds)
        {
            try
            {
                var xml = await httpClient.GetStringAsync(feed.Url);
                var doc = XDocument.Parse(xml);

                // Try RSS 2.0
                var items = doc.Descendants("item").Take(3).ToList();
                // Try Atom
                if (items.Count == 0)
                {
                    XNamespace atom = "http://www.w3.org/2005/Atom";
                    items = doc.Descendants(atom + "entry").Take(3).ToList();
                }

                if (items.Count == 0) continue;

                var firstItem = items.First();
                var itemId = GetItemId(firstItem);

                if (itemId == feed.LastItemId) continue;

                if (string.IsNullOrEmpty(feed.LastItemId))
                {
                    await UpdateLastItemId(feed.Id, itemId);
                    continue;
                }

                var title = GetElementValue(firstItem, "title") ?? "New Item";
                var link = GetElementValue(firstItem, "link") ?? GetAtomLink(firstItem) ?? "";
                var desc = GetElementValue(firstItem, "description")
                           ?? GetElementValue(firstItem, "summary")
                           ?? GetElementValue(firstItem, "content");

                var guild = _client.GetGuild(feed.GuildId);
                var ch = guild?.GetTextChannel(feed.ChannelId);
                if (ch is null) continue;

                var embed = _sender.CreateEmbed()
                    .WithTitle(title.StripHtml().TrimTo(256))
                    .WithDescription(desc?.StripHtml()?.TrimTo(2048) ?? "")
                    .WithFooter(feed.Url.TrimTo(128))
                    .WithOkColor();

                if (Uri.IsWellFormedUriString(link, UriKind.Absolute))
                    embed.WithUrl(link);

                await _sender.Response(ch).Embed(embed).SendAsync();
                await UpdateLastItemId(feed.Id, itemId);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error polling RSS feed {Url}", feed.Url);
            }
        }
    }

    private static string GetItemId(XElement item)
    {
        return GetElementValue(item, "guid")
               ?? GetElementValue(item, "id")
               ?? GetElementValue(item, "link")
               ?? GetAtomLink(item)
               ?? "";
    }

    private static string GetElementValue(XElement parent, string name)
    {
        var el = parent.Element(name);
        if (el is not null) return el.Value;

        // Try with Atom namespace
        XNamespace atom = "http://www.w3.org/2005/Atom";
        el = parent.Element(atom + name);
        return el?.Value;
    }

    private static string GetAtomLink(XElement item)
    {
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var link = item.Element(atom + "link");
        return link?.Attribute("href")?.Value;
    }

    private async Task UpdateLastItemId(int id, string itemId)
    {
        await using var uow = _db.GetDbContext();
        await uow.GetTable<RssFeedEntry>()
            .Where(x => x.Id == id)
            .UpdateAsync(x => new RssFeedEntry { LastItemId = itemId });
    }

    public async Task<(bool success, string title)> TestFeedAsync(string url)
    {
        try
        {
            using var httpClient = _http.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "SantiBot/1.0");
            var xml = await httpClient.GetStringAsync(url);
            var doc = XDocument.Parse(xml);

            var channelTitle = doc.Descendants("title").FirstOrDefault()?.Value ?? "Unknown Feed";
            var items = doc.Descendants("item").ToList();
            if (items.Count == 0)
            {
                XNamespace atom = "http://www.w3.org/2005/Atom";
                items = doc.Descendants(atom + "entry").ToList();
            }

            return (true, $"{channelTitle} ({items.Count} items)");
        }
        catch
        {
            return (false, null);
        }
    }

    public async Task<int> AddFeedAsync(ulong guildId, ulong channelId, string url)
    {
        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<RssFeedEntry>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.Url == url);
        if (exists) return -1;

        var entry = await uow.GetTable<RssFeedEntry>()
            .InsertWithOutputAsync(() => new RssFeedEntry
            {
                GuildId = guildId,
                ChannelId = channelId,
                Url = url
            });
        return entry.Id;
    }

    public async Task<bool> RemoveFeedAsync(ulong guildId, int feedId)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<RssFeedEntry>()
            .Where(x => x.GuildId == guildId && x.Id == feedId)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<RssFeedEntry>> ListFeedsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<RssFeedEntry>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
