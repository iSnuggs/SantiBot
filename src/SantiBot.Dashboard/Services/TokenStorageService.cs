using System.Collections.Concurrent;

namespace SantiBot.Dashboard.Services;

/// <summary>
/// Stores Discord OAuth access tokens in memory, keyed by user ID.
/// When a user logs in via Discord OAuth, we save their token here so we can
/// call the Discord API on their behalf (e.g., fetching their server list).
/// Tokens auto-expire based on Discord's expiry time.
/// </summary>
public class TokenStorageService
{
    private readonly ConcurrentDictionary<ulong, StoredToken> _tokens = new();

    /// <summary>
    /// Save a user's Discord access token after they log in.
    /// </summary>
    public void StoreToken(ulong userId, string accessToken, string refreshToken, int expiresInSeconds)
    {
        _tokens[userId] = new StoredToken
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds),
        };
    }

    /// <summary>
    /// Get the stored Discord token for a user. Returns null if expired or not found.
    /// </summary>
    public string? GetAccessToken(ulong userId)
    {
        if (!_tokens.TryGetValue(userId, out var stored))
            return null;

        // If the token has expired, remove it and return null
        if (DateTime.UtcNow >= stored.ExpiresAt)
        {
            _tokens.TryRemove(userId, out _);
            return null;
        }

        return stored.AccessToken;
    }

    /// <summary>
    /// Remove a user's stored token (e.g., on logout).
    /// </summary>
    public void RemoveToken(ulong userId)
    {
        _tokens.TryRemove(userId, out _);
    }

    /// <summary>
    /// Clean up any expired tokens to free memory.
    /// Called periodically or on demand.
    /// </summary>
    public int CleanupExpired()
    {
        var now = DateTime.UtcNow;
        var removed = 0;

        foreach (var kvp in _tokens)
        {
            if (now >= kvp.Value.ExpiresAt)
            {
                if (_tokens.TryRemove(kvp.Key, out _))
                    removed++;
            }
        }

        return removed;
    }
}

/// <summary>
/// Holds a Discord OAuth token along with its expiry time.
/// </summary>
public class StoredToken
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
