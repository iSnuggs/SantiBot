#nullable disable

namespace SantiBot.Modules.Utility.DashboardApi.MobileApp;

/// <summary>
/// PWA enhancements service. Provides manifest data and service worker config.
/// The dashboard already has PWA support, this enhances the API to support offline caching headers.
///
/// Dashboard should serve these endpoints:
///   GET /manifest.json -- PWA manifest
///   GET /sw.js -- Service worker
///   GET /api/pwa/config -- Cache strategy configuration
/// </summary>
public sealed class MobileAppService : INService
{
    private readonly DiscordSocketClient _client;

    public MobileAppService(DiscordSocketClient client)
    {
        _client = client;
    }

    public record PwaManifest(
        string Name,
        string ShortName,
        string Description,
        string StartUrl,
        string Display,
        string ThemeColor,
        string BackgroundColor,
        List<PwaIcon> Icons);

    public record PwaIcon(string Src, string Sizes, string Type);

    public PwaManifest GetManifest()
    {
        return new PwaManifest(
            Name: "SantiBot Dashboard",
            ShortName: "SantiBot",
            Description: "Manage your SantiBot Discord server settings",
            StartUrl: "/",
            Display: "standalone",
            ThemeColor: "#5865F2",
            BackgroundColor: "#1a1a2e",
            Icons:
            [
                new PwaIcon("/icons/icon-192.png", "192x192", "image/png"),
                new PwaIcon("/icons/icon-512.png", "512x512", "image/png")
            ]);
    }

    public record CacheConfig(
        string Strategy,
        int MaxAge,
        List<string> PrecacheUrls,
        List<string> CacheFirstPatterns,
        List<string> NetworkFirstPatterns);

    public CacheConfig GetCacheConfig()
    {
        return new CacheConfig(
            Strategy: "stale-while-revalidate",
            MaxAge: 3600,
            PrecacheUrls: ["/", "/dashboard", "/api/health"],
            CacheFirstPatterns: ["/static/*", "/icons/*", "*.css", "*.js"],
            NetworkFirstPatterns: ["/api/*"]);
    }

    public record AppStatus(
        bool IsOnline,
        int GuildCount,
        int ShardCount,
        string BotVersion);

    public AppStatus GetStatus()
    {
        return new AppStatus(
            IsOnline: _client.ConnectionState == Discord.ConnectionState.Connected,
            GuildCount: _client.Guilds.Count,
            ShardCount: 1,
            BotVersion: "1.0.0");
    }
}
