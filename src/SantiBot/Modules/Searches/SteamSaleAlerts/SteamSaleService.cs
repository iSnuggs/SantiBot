#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Searches.SteamSaleAlerts;

public sealed class SteamSaleService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private readonly ShardData _shardData;

    public SteamSaleService(
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
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await PollSteamSales();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error polling Steam sales");
            }
        }
    }

    private async Task PollSteamSales()
    {
        List<SteamSaleWatch> watches;
        await using (var uow = _db.GetDbContext())
        {
            watches = await uow.GetTable<SteamSaleWatch>()
                .Where(x => Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
                .ToListAsyncLinqToDB();
        }

        if (watches.Count == 0) return;

        using var httpClient = _http.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SantiBot/1.0");

        var grouped = watches.GroupBy(x => x.AppId);
        foreach (var group in grouped)
        {
            try
            {
                var appId = group.Key;
                var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&filters=price_overview";
                var resp = await httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(resp);

                if (!doc.RootElement.TryGetProperty(appId, out var appData))
                    continue;
                if (!appData.TryGetProperty("success", out var success) || !success.GetBoolean())
                    continue;
                if (!appData.TryGetProperty("data", out var data))
                    continue;
                if (!data.TryGetProperty("price_overview", out var price))
                    continue;

                var discountPercent = price.GetProperty("discount_percent").GetInt32();
                var isOnSale = discountPercent > 0;
                var finalPrice = price.GetProperty("final_formatted").GetString();
                var initialPrice = price.GetProperty("initial_formatted").GetString();

                foreach (var watch in group)
                {
                    if (isOnSale && !watch.LastOnSale)
                    {
                        var guild = _client.GetGuild(watch.GuildId);
                        var ch = guild?.GetTextChannel(watch.ChannelId);
                        if (ch is null) continue;

                        var embed = _sender.CreateEmbed()
                            .WithTitle($"Steam Sale Alert: {watch.GameName}")
                            .WithDescription(
                                $"**{watch.GameName}** is now **{discountPercent}% off**!\n" +
                                $"~~{initialPrice}~~ -> **{finalPrice}**")
                            .WithUrl($"https://store.steampowered.com/app/{appId}")
                            .WithThumbnailUrl($"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/header.jpg")
                            .WithOkColor();

                        await _sender.Response(ch).Embed(embed).SendAsync();
                    }

                    // Update sale status
                    await using var uow = _db.GetDbContext();
                    await uow.GetTable<SteamSaleWatch>()
                        .Where(x => x.Id == watch.Id)
                        .UpdateAsync(x => new SteamSaleWatch { LastOnSale = isOnSale });
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error polling Steam sale for app {AppId}", group.Key);
            }
        }
    }

    public async Task<bool> WatchAsync(ulong guildId, ulong channelId, string appId, string gameName)
    {
        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<SteamSaleWatch>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.AppId == appId);
        if (exists) return false;

        await uow.GetTable<SteamSaleWatch>()
            .InsertAsync(() => new SteamSaleWatch
            {
                GuildId = guildId,
                ChannelId = channelId,
                AppId = appId,
                GameName = gameName
            });
        return true;
    }

    public async Task<bool> UnwatchAsync(ulong guildId, string appId)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<SteamSaleWatch>()
            .Where(x => x.GuildId == guildId && x.AppId == appId)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<SteamSaleWatch>> ListAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<SteamSaleWatch>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<string> ResolveGameNameAsync(string appId)
    {
        try
        {
            using var httpClient = _http.CreateClient();
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&filters=basic";
            var resp = await httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(resp);
            if (doc.RootElement.TryGetProperty(appId, out var appData) &&
                appData.TryGetProperty("data", out var data) &&
                data.TryGetProperty("name", out var name))
                return name.GetString();
        }
        catch { }
        return $"App {appId}";
    }
}
