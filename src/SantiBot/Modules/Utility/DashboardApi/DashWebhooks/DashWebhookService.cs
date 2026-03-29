#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Utility.DashboardApi.DashWebhooks;

/// <summary>
/// Dashboard webhooks -- fire HTTP webhooks on dashboard events.
/// Events: config_changed, member_warned, member_banned, role_changed, command_used
/// </summary>
public sealed class DashWebhookService : INService
{
    private readonly DbService _db;
    private readonly IHttpClientFactory _http;

    public DashWebhookService(DbService db, IHttpClientFactory http)
    {
        _db = db;
        _http = http;
    }

    public async Task<int> AddWebhookAsync(ulong guildId, string url, string eventName)
    {
        await using var uow = _db.GetDbContext();
        var hook = await uow.GetTable<DashWebhook>()
            .InsertWithOutputAsync(() => new DashWebhook
            {
                GuildId = guildId,
                Url = url,
                Event = eventName.ToLower(),
                IsEnabled = true
            });
        return hook.Id;
    }

    public async Task<bool> RemoveWebhookAsync(ulong guildId, int hookId)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<DashWebhook>()
            .Where(x => x.GuildId == guildId && x.Id == hookId)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<DashWebhook>> ListWebhooksAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<DashWebhook>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task FireWebhookAsync(ulong guildId, string eventName, object payload)
    {
        await using var uow = _db.GetDbContext();
        var hooks = await uow.GetTable<DashWebhook>()
            .Where(x => x.GuildId == guildId && x.IsEnabled && x.Event == eventName.ToLower())
            .ToListAsyncLinqToDB();

        if (hooks.Count == 0) return;

        using var httpClient = _http.CreateClient();
        var json = JsonSerializer.Serialize(new
        {
            @event = eventName,
            guild_id = guildId.ToString(),
            timestamp = DateTime.UtcNow,
            data = payload
        });

        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        foreach (var hook in hooks)
        {
            try
            {
                await httpClient.PostAsync(hook.Url, content);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to fire webhook {WebhookId} to {Url}", hook.Id, hook.Url);
            }
        }
    }
}
