#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Searches.CryptoAlerts;

public sealed class CryptoAlertService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IHttpClientFactory _http;
    private readonly ShardData _shardData;

    private const string COINGECKO_API = "https://api.coingecko.com/api/v3/simple/price";

    public CryptoAlertService(
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
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(3));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await CheckAlerts();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error checking crypto alerts");
            }
        }
    }

    private async Task CheckAlerts()
    {
        List<CryptoAlert> alerts;
        await using (var uow = _db.GetDbContext())
        {
            alerts = await uow.GetTable<CryptoAlert>()
                .Where(x => !x.Triggered &&
                            Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
                .ToListAsyncLinqToDB();
        }

        if (alerts.Count == 0) return;

        var coinIds = alerts.Select(x => x.CoinId.ToLower()).Distinct().ToList();
        var idsParam = string.Join(",", coinIds);

        using var httpClient = _http.CreateClient();
        var url = $"{COINGECKO_API}?ids={idsParam}&vs_currencies=usd";
        var resp = await httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(resp);

        foreach (var alert in alerts)
        {
            if (!doc.RootElement.TryGetProperty(alert.CoinId.ToLower(), out var coinData))
                continue;
            if (!coinData.TryGetProperty("usd", out var priceEl))
                continue;

            var currentPrice = priceEl.GetDecimal();
            var triggered = alert.Direction.ToLower() == "above"
                ? currentPrice >= alert.TargetPrice
                : currentPrice <= alert.TargetPrice;

            if (!triggered) continue;

            // Mark as triggered
            await using var uow = _db.GetDbContext();
            await uow.GetTable<CryptoAlert>()
                .Where(x => x.Id == alert.Id)
                .UpdateAsync(x => new CryptoAlert { Triggered = true });

            // Send notification
            var guild = _client.GetGuild(alert.GuildId);
            var ch = guild?.GetTextChannel(alert.ChannelId);
            if (ch is null) continue;

            var dirText = alert.Direction.ToLower() == "above" ? "risen above" : "fallen below";
            var embed = _sender.CreateEmbed()
                .WithTitle($"Crypto Alert: {alert.CoinId.ToUpper()}")
                .WithDescription(
                    $"<@{alert.UserId}> **{alert.CoinId.ToUpper()}** has {dirText} " +
                    $"**${alert.TargetPrice:N2}**!\nCurrent price: **${currentPrice:N2}**")
                .WithOkColor();

            await _sender.Response(ch).Embed(embed).SendAsync();
        }
    }

    public async Task<int> AddAlertAsync(ulong guildId, ulong channelId, ulong userId,
        string coinId, string direction, decimal targetPrice)
    {
        coinId = coinId.ToLower();
        await using var uow = _db.GetDbContext();
        var alert = await uow.GetTable<CryptoAlert>()
            .InsertWithOutputAsync(() => new CryptoAlert
            {
                GuildId = guildId,
                ChannelId = channelId,
                UserId = userId,
                CoinId = coinId,
                Direction = direction.ToLower(),
                TargetPrice = targetPrice,
                Triggered = false
            });
        return alert.Id;
    }

    public async Task<bool> RemoveAlertAsync(ulong guildId, int alertId)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<CryptoAlert>()
            .Where(x => x.GuildId == guildId && x.Id == alertId)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<CryptoAlert>> ListAlertsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<CryptoAlert>()
            .Where(x => x.GuildId == guildId && !x.Triggered)
            .ToListAsyncLinqToDB();
    }
}
