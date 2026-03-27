using System.Collections.Concurrent;

namespace SantiBot.Dashboard.Services;

/// <summary>
/// Checks whether a user has permission to manage a specific Discord guild.
/// Caches the results for a few minutes so we don't hit Discord's API on every request.
/// </summary>
public class GuildPermissionService
{
    private readonly DiscordOAuthService _oauth;
    private readonly TokenStorageService _tokenStorage;

    // Cache: userId -> (set of manageable guild IDs, expiry time)
    private readonly ConcurrentDictionary<ulong, CachedGuildList> _cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public GuildPermissionService(DiscordOAuthService oauth, TokenStorageService tokenStorage)
    {
        _oauth = oauth;
        _tokenStorage = tokenStorage;
    }

    /// <summary>
    /// Check if a user can manage a specific guild.
    /// Returns true if allowed, false if not, null if we can't verify (expired token).
    /// </summary>
    public async Task<bool?> CanManageGuildAsync(ulong userId, ulong guildId)
    {
        // Check cache first — avoids hitting Discord API on every request
        if (_cache.TryGetValue(userId, out var cached) && DateTime.UtcNow < cached.ExpiresAt)
            return cached.GuildIds.Contains(guildId);

        // Get the user's Discord token
        var accessToken = _tokenStorage.GetAccessToken(userId);
        if (accessToken is null)
            return null; // Token expired, user needs to re-login

        // Fetch their guilds from Discord and cache the result
        var guilds = await _oauth.GetUserGuildsAsync(accessToken);
        var manageableIds = guilds
            .Where(g => g.CanManage)
            .Select(g => ulong.TryParse(g.Id, out var id) ? id : 0)
            .Where(id => id != 0)
            .ToHashSet();

        _cache[userId] = new CachedGuildList
        {
            GuildIds = manageableIds,
            ExpiresAt = DateTime.UtcNow.Add(CacheDuration),
        };

        return manageableIds.Contains(guildId);
    }

    /// <summary>
    /// Clear the cache for a user (e.g., on logout or token refresh).
    /// </summary>
    public void InvalidateCache(ulong userId)
    {
        _cache.TryRemove(userId, out _);
    }
}

internal class CachedGuildList
{
    public HashSet<ulong> GuildIds { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}
