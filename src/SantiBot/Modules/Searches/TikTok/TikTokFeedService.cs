#nullable disable
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Searches;

/// <summary>
/// TikTok feed notifications using yt-dlp to poll TikTok profiles for new videos.
/// No external RSS service needed — just yt-dlp (already required for music).
/// </summary>
public sealed class TikTokFeedService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private Timer _timer;

    private static readonly Regex _tiktokUserRegex = new(
        @"(?:tiktok\.com/@|^@?)([a-zA-Z0-9_.]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TikTokFeedService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public Task OnReadyAsync()
    {
        // Check for new TikTok videos every 10 minutes
        _timer = new Timer(async _ => await CheckAllFeedsAsync(), null,
            TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));

        Log.Information("TikTok feed checker started (10 minute interval)");
        return Task.CompletedTask;
    }

    private async Task CheckAllFeedsAsync()
    {
        try
        {
            await using var uow = _db.GetDbContext();
            var follows = await uow.Set<TikTokFollow>()
                .ToListAsyncEF();

            if (follows.Count == 0)
                return;

            // Group by username to avoid duplicate API calls
            var byUser = follows.GroupBy(f => f.Username.ToLowerInvariant());

            foreach (var group in byUser)
            {
                try
                {
                    var username = group.Key;
                    var latestVideoId = await GetLatestVideoIdAsync(username);

                    if (string.IsNullOrEmpty(latestVideoId))
                        continue;

                    foreach (var follow in group)
                    {
                        // Skip if we already notified about this video
                        if (follow.LastVideoId == latestVideoId)
                            continue;

                        // First time following — just save the ID, don't notify
                        if (string.IsNullOrEmpty(follow.LastVideoId))
                        {
                            follow.LastVideoId = latestVideoId;
                            continue;
                        }

                        // New video — send notification!
                        var guild = _client.GetGuild(follow.GuildId);
                        var channel = guild?.GetTextChannel(follow.ChannelId);

                        if (channel is not null)
                        {
                            var videoUrl = $"https://www.tiktok.com/@{username}/video/{latestVideoId}";

                            var embed = _sender.CreateEmbed(follow.GuildId)
                                .WithTitle($"🎵 New TikTok from @{username}")
                                .WithUrl(videoUrl)
                                .WithDescription($"**@{username}** just posted a new TikTok!")
                                .WithColor(new Discord.Color(0xFF0050)) // TikTok pink
                                .WithTimestamp(DateTime.UtcNow);

                            await channel.SendMessageAsync(videoUrl, embed: embed.Build());
                        }

                        follow.LastVideoId = latestVideoId;
                    }

                    await uow.SaveChangesAsync();

                    // Rate limit friendly — wait between users
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "TikTok feed check failed for @{Username}", group.Key);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "TikTok feed checker error");
        }
    }

    /// <summary>
    /// Gets the latest video ID from a TikTok user using yt-dlp.
    /// </summary>
    private async Task<string> GetLatestVideoIdAsync(string username)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--js-runtimes node --flat-playlist --playlist-items 1 --print id \"https://www.tiktok.com/@{username}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var videoId = output?.Trim().Split('\n').FirstOrDefault()?.Trim();
            return string.IsNullOrWhiteSpace(videoId) ? null : videoId;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "yt-dlp TikTok fetch failed for @{Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Extracts a TikTok username from various input formats.
    /// </summary>
    public static string ParseUsername(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var match = _tiktokUserRegex.Match(input.Trim());
        return match.Success ? match.Groups[1].Value : null;
    }

    // ── Public API ──

    public async Task<(bool success, string error)> FollowAsync(ulong guildId, ulong channelId, string input)
    {
        var username = ParseUsername(input);
        if (username is null)
            return (false, "Invalid TikTok username. Use @username or a TikTok profile URL.");

        // Verify the user exists by trying to fetch their latest video
        var testId = await GetLatestVideoIdAsync(username);
        if (testId is null)
            return (false, $"Could not find TikTok user @{username}. Check the spelling.");

        await using var uow = _db.GetDbContext();

        // Check if already following
        var existing = await uow.Set<TikTokFollow>()
            .FirstOrDefaultAsyncEF(f => f.GuildId == guildId && f.ChannelId == channelId
                && f.Username.ToLower() == username.ToLower());

        if (existing is not null)
            return (false, $"This channel is already following @{username}.");

        uow.Set<TikTokFollow>().Add(new TikTokFollow
        {
            GuildId = guildId,
            ChannelId = channelId,
            Username = username,
            LastVideoId = testId, // Save current latest so we don't notify for old videos
        });

        await uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> UnfollowAsync(ulong guildId, ulong channelId, string input)
    {
        var username = ParseUsername(input);
        if (username is null)
            return false;

        await using var uow = _db.GetDbContext();
        var follow = await uow.Set<TikTokFollow>()
            .FirstOrDefaultAsyncEF(f => f.GuildId == guildId && f.ChannelId == channelId
                && f.Username.ToLower() == username.ToLower());

        if (follow is null)
            return false;

        uow.Set<TikTokFollow>().Remove(follow);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<List<TikTokFollow>> GetFollowsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<TikTokFollow>()
            .AsNoTracking()
            .Where(f => f.GuildId == guildId)
            .ToListAsyncEF();
    }
}
