#nullable disable
using Discord.WebSocket;

namespace SantiBot.Modules.Searches;

/// <summary>
/// Reads Spotify listening activity from Discord presence data.
/// No API key needed — Discord exposes this when users have Spotify connected.
/// </summary>
public sealed class SpotifyNowPlayingService : INService
{
    private readonly DiscordSocketClient _client;

    public SpotifyNowPlayingService(DiscordSocketClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets the Spotify activity for a guild user, or null if they aren't listening.
    /// </summary>
    public SpotifyInfo GetSpotifyActivity(SocketGuild guild, ulong userId)
    {
        var user = guild.GetUser(userId);
        if (user is null)
            return null;

        foreach (var activity in user.Activities)
        {
            if (activity is SpotifyGame spotify)
            {
                return new SpotifyInfo
                {
                    TrackTitle = spotify.TrackTitle,
                    Artists = string.Join(", ", spotify.Artists ?? Enumerable.Empty<string>()),
                    AlbumTitle = spotify.AlbumTitle,
                    AlbumArtUrl = spotify.AlbumArtUrl,
                    TrackUrl = spotify.TrackUrl,
                    Duration = spotify.Duration,
                    Elapsed = spotify.Elapsed,
                };
            }
        }

        return null;
    }
}

public class SpotifyInfo
{
    public string TrackTitle { get; set; }
    public string Artists { get; set; }
    public string AlbumTitle { get; set; }
    public string AlbumArtUrl { get; set; }
    public string TrackUrl { get; set; }
    public TimeSpan? Duration { get; set; }
    public TimeSpan? Elapsed { get; set; }
}
