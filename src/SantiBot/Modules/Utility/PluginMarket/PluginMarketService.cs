#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Utility.PluginMarket;

/// <summary>
/// Plugin marketplace -- browse/install Medusa plugins from a registry.
/// Registry is stored as a JSON file. Community plugins can be listed.
/// </summary>
public sealed class PluginMarketService : INService
{
    private readonly DbService _db;
    private readonly IHttpClientFactory _http;

    // Built-in plugin registry
    private static readonly List<PluginInfo> Registry =
    [
        new("leveling-plus", "1.0.0", "Enhanced XP and leveling system with custom curves", "SantiBot Team", ["xp", "leveling"]),
        new("economy-plus", "1.0.0", "Advanced economy features: stocks, businesses, trading", "SantiBot Team", ["economy", "trading"]),
        new("music-plus", "1.0.0", "Enhanced music player with Spotify and SoundCloud support", "SantiBot Team", ["music", "audio"]),
        new("auto-mod-plus", "1.0.0", "Advanced auto-moderation with AI-powered content filtering", "SantiBot Team", ["moderation", "automod"]),
        new("welcome-plus", "1.0.0", "Advanced welcome system with image cards and verification", "SantiBot Team", ["welcome", "greet"]),
        new("ticket-system", "1.0.0", "Support ticket system with categories and transcripts", "Community", ["support", "tickets"]),
        new("giveaway-plus", "1.0.0", "Enhanced giveaways with requirements and multiple winners", "Community", ["giveaway", "events"]),
        new("reaction-roles-plus", "1.0.0", "Advanced reaction roles with button menus and dropdowns", "Community", ["roles", "reactions"]),
        new("starboard", "1.0.0", "Starboard feature - highlight popular messages", "Community", ["social", "starboard"]),
        new("poll-plus", "1.0.0", "Advanced polls with timed voting and multiple choice", "Community", ["poll", "voting"]),
    ];

    public record PluginInfo(
        string Name,
        string Version,
        string Description,
        string Author,
        List<string> Tags);

    public PluginMarketService(DbService db, IHttpClientFactory http)
    {
        _db = db;
        _http = http;
    }

    public List<PluginInfo> SearchPlugins(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Registry;

        return Registry
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       p.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public PluginInfo GetPluginInfo(string name)
    {
        return Registry.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> InstallPluginAsync(ulong guildId, string pluginName)
    {
        var plugin = Registry.FirstOrDefault(p =>
            p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        if (plugin is null) return false;

        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<InstalledPlugin>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.PluginName == plugin.Name);
        if (exists) return false;

        await uow.GetTable<InstalledPlugin>()
            .InsertAsync(() => new InstalledPlugin
            {
                GuildId = guildId,
                PluginName = plugin.Name,
                Version = plugin.Version,
                IsEnabled = true
            });
        return true;
    }

    public async Task<bool> UninstallPluginAsync(ulong guildId, string pluginName)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<InstalledPlugin>()
            .Where(x => x.GuildId == guildId &&
                       x.PluginName.ToLower() == pluginName.ToLower())
            .DeleteAsync();
        return count > 0;
    }

    public async Task<List<InstalledPlugin>> ListInstalledAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<InstalledPlugin>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
