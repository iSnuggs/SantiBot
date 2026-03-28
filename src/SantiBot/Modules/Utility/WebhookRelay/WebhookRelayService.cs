using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class WebhookRelayService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly NonBlocking.ConcurrentDictionary<string, WebhookRelayConfig> _endpoints = new();

    public WebhookRelayService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;

        _ = Task.Run(LoadEndpointsAsync);
    }

    private async Task LoadEndpointsAsync()
    {
        await using var ctx = _db.GetDbContext();
        var configs = await ctx.GetTable<WebhookRelayConfig>()
            .Where(x => x.Enabled)
            .ToListAsyncLinqToDB();

        foreach (var c in configs)
            _endpoints[c.EndpointId] = c;
    }

    public async Task<WebhookRelayConfig> CreateEndpointAsync(ulong guildId, ulong channelId)
    {
        var endpointId = Guid.NewGuid().ToString("N")[..12];

        var config = new WebhookRelayConfig
        {
            GuildId = guildId,
            TargetChannelId = channelId,
            EndpointId = endpointId,
            Enabled = true
        };

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<WebhookRelayConfig>().InsertAsync(() => new WebhookRelayConfig
        {
            GuildId = guildId,
            TargetChannelId = channelId,
            EndpointId = endpointId,
            Enabled = true,
            SecretKey = "",
            FilterField = ""
        });

        _endpoints[endpointId] = config;
        return config;
    }

    public async Task<List<WebhookRelayConfig>> ListEndpointsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<WebhookRelayConfig>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> RemoveEndpointAsync(ulong guildId, string endpointId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<WebhookRelayConfig>()
            .Where(x => x.GuildId == guildId && x.EndpointId == endpointId)
            .DeleteAsync();

        _endpoints.TryRemove(endpointId, out _);
        return deleted > 0;
    }

    public async Task<bool> SetSecretAsync(ulong guildId, string endpointId, string secret)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<WebhookRelayConfig>()
            .Where(x => x.GuildId == guildId && x.EndpointId == endpointId)
            .Set(x => x.SecretKey, secret)
            .UpdateAsync();

        if (_endpoints.TryGetValue(endpointId, out var config))
            config.SecretKey = secret;

        return updated > 0;
    }

    /// <summary>
    /// Called by the ASP.NET webhook endpoint controller to relay a payload to Discord.
    /// </summary>
    public async Task<bool> RelayPayloadAsync(string endpointId, string body, string? authHeader)
    {
        if (!_endpoints.TryGetValue(endpointId, out var config) || !config.Enabled)
            return false;

        // Validate secret if set
        if (!string.IsNullOrEmpty(config.SecretKey))
        {
            if (authHeader != config.SecretKey)
                return false;
        }

        try
        {
            var guild = _client.GetGuild(config.GuildId);
            var channel = guild?.GetTextChannel(config.TargetChannelId);
            if (channel is null)
                return false;

            // Extract a specific field if configured, otherwise use the full body
            var displayText = body;
            if (!string.IsNullOrEmpty(config.FilterField))
            {
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(body);
                    if (json.RootElement.TryGetProperty(config.FilterField, out var field))
                        displayText = field.ToString();
                }
                catch
                {
                    // If JSON parse fails, use full body
                }
            }

            // Truncate if too long for an embed
            if (displayText.Length > 4000)
                displayText = displayText[..4000] + "...";

            var eb = new EmbedBuilder()
                .WithTitle($"Webhook: {config.EndpointId}")
                .WithDescription(displayText)
                .WithColor(new Color(0x3498db))
                .WithCurrentTimestamp();

            await channel.SendMessageAsync(embed: eb.Build());
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error relaying webhook for endpoint {EndpointId}", endpointId);
            return false;
        }
    }
}
