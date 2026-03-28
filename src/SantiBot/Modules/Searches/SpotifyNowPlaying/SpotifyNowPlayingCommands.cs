#nullable disable
using Discord.WebSocket;
using SantiBot.Common;

namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class SpotifyNowPlayingCommands : SantiModule<SpotifyNowPlayingService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Spotify(IUser target = null)
        {
            target ??= ctx.User;

            var guild = (SocketGuild)ctx.Guild;
            var info = _service.GetSpotifyActivity(guild, target.Id);

            if (info is null)
            {
                if (target.Id == ctx.User.Id)
                    await Response().Error(strs.spotify_not_listening_self).SendAsync();
                else
                    await Response().Error(strs.spotify_not_listening(target.Mention)).SendAsync();
                return;
            }

            // Build a progress bar for the track
            var progressBar = "";
            if (info.Duration.HasValue && info.Elapsed.HasValue)
            {
                var pct = info.Elapsed.Value.TotalSeconds / info.Duration.Value.TotalSeconds;
                var filled = (int)(pct * 15);
                progressBar = new string('▓', filled) + new string('░', 15 - filled);
                var elapsed = info.Elapsed.Value;
                var duration = info.Duration.Value;
                progressBar = $"`{elapsed:m\\:ss}` {progressBar} `{duration:m\\:ss}`";
            }

            var embed = CreateEmbed()
                .WithTitle($"🎵 {info.TrackTitle}")
                .WithUrl(info.TrackUrl ?? "https://open.spotify.com")
                .WithColor(new Discord.Color(0x1DB954)) // Spotify green
                .AddField("Artist", info.Artists ?? "Unknown", true)
                .AddField("Album", info.AlbumTitle ?? "Unknown", true)
                .WithFooter($"Listening on Spotify • {target.Username}", target.GetAvatarUrl());

            if (!string.IsNullOrEmpty(progressBar))
                embed.AddField("Progress", progressBar, false);

            if (!string.IsNullOrEmpty(info.AlbumArtUrl))
                embed.WithThumbnailUrl(info.AlbumArtUrl);

            await Response().Embed(embed).SendAsync();
        }
    }
}
