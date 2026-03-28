using System.Text.RegularExpressions;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class RaidScoreService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    // Track recent joins per guild for raid-window detection: guildId -> list of join times
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, List<DateTimeOffset>> _recentJoins = new();

    // Spam-like username patterns
    private static readonly Regex _spamPattern = new(
        @"(free|nitro|gift|discord\.gg|steam|airdrop)\d*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public RaidScoreService(DbService db, DiscordSocketClient client, IBotCreds creds)
    {
        _db = db;
        _client = client;
        _creds = creds;
    }

    public Task OnReadyAsync()
    {
        _client.UserJoined += OnUserJoined;
        return Task.CompletedTask;
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        try
        {
            var config = await GetConfigAsync(user.Guild.Id);
            if (config is null || !config.Enabled)
                return;

            // Track this join for raid-window detection
            TrackJoin(user.Guild.Id);

            var score = CalculateScore(user, user.Guild.Id);

            if (score < config.ThresholdScore)
                return;

            await TakeActionAsync(user, config, score);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in RaidScore for guild {GuildId}", user.Guild.Id);
        }
    }

    /// <summary>
    /// Calculate a risk score for a user (0-100+). Higher = more suspicious.
    /// </summary>
    public int CalculateScore(SocketGuildUser user, ulong guildId)
    {
        var score = 0;
        var accountAge = DateTimeOffset.UtcNow - user.CreatedAt;

        // Account age checks
        if (accountAge.TotalDays < 1)
            score += 40;
        else if (accountAge.TotalDays < 7)
            score += 20;

        // No avatar
        if (user.GetAvatarUrl() is null)
            score += 15;

        // Username contains spam patterns or excessive numbers
        if (_spamPattern.IsMatch(user.Username))
            score += 10;

        // No bio/about me (not directly available via gateway, but no global name is a signal)
        if (string.IsNullOrEmpty(user.GlobalName))
            score += 5;

        // Raid window: 10+ joins in last 5 minutes
        if (IsRaidWindow(guildId))
            score += 30;

        return Math.Min(score, 100);
    }

    private void TrackJoin(ulong guildId)
    {
        var now = DateTimeOffset.UtcNow;
        var joins = _recentJoins.GetOrAdd(guildId, _ => new List<DateTimeOffset>());

        lock (joins)
        {
            joins.Add(now);
            // Keep only last 5 minutes
            joins.RemoveAll(j => (now - j).TotalMinutes > 5);
        }
    }

    private bool IsRaidWindow(ulong guildId)
    {
        if (!_recentJoins.TryGetValue(guildId, out var joins))
            return false;

        lock (joins)
        {
            return joins.Count >= 10;
        }
    }

    private async Task TakeActionAsync(SocketGuildUser user, RaidScoreConfig config, int score)
    {
        var action = config.ActionOnThreshold?.ToLowerInvariant() ?? "alert";

        switch (action)
        {
            case "ban":
                try { await user.BanAsync(reason: $"Raid score {score} exceeded threshold {config.ThresholdScore}"); }
                catch { /* Missing permissions */ }
                break;

            case "kick":
                try { await user.KickAsync($"Raid score {score} exceeded threshold {config.ThresholdScore}"); }
                catch { /* Missing permissions */ }
                break;

            case "quarantine":
                // Quarantine would assign a quarantine role if the Quarantine module is set up.
                // For now, fall through to alert.
                break;
        }

        // Always send an alert if the channel is configured
        if (config.AlertChannelId is { } channelId)
        {
            var guild = user.Guild;
            if (guild.GetTextChannel(channelId) is { } alertCh)
            {
                var embed = new EmbedBuilder()
                    .WithColor(score >= 80 ? Color.Red : Color.Orange)
                    .WithTitle("Raid Score Alert")
                    .WithDescription($"{user.Mention} ({user.Username}) joined with a risk score of **{score}**.")
                    .AddField("Action Taken", action, true)
                    .AddField("Threshold", config.ThresholdScore.ToString(), true)
                    .AddField("Account Age", $"{(DateTimeOffset.UtcNow - user.CreatedAt).TotalHours:F1} hours", true)
                    .WithCurrentTimestamp()
                    .Build();

                try { await alertCh.SendMessageAsync(embed: embed); }
                catch { /* Missing permissions */ }
            }
        }
    }

    // ── Config CRUD ──

    public async Task<RaidScoreConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<RaidScoreConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<RaidScoreConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<RaidScoreConfig>()
                .InsertAsync(() => new RaidScoreConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                });
        }
        else
        {
            await ctx.GetTable<RaidScoreConfig>()
                .Where(x => x.GuildId == guildId)
                .UpdateAsync(x => new RaidScoreConfig { Enabled = enabled });
        }
    }

    public async Task SetThresholdAsync(ulong guildId, int threshold)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<RaidScoreConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new RaidScoreConfig { ThresholdScore = threshold });
    }

    public async Task SetActionAsync(ulong guildId, string action)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<RaidScoreConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new RaidScoreConfig { ActionOnThreshold = action });
    }

    public async Task SetAlertChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<RaidScoreConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new RaidScoreConfig { AlertChannelId = channelId });
    }

    private async Task EnsureConfigAsync(SantiContext ctx, ulong guildId)
    {
        var exists = await ctx.GetTable<RaidScoreConfig>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId);

        if (!exists)
        {
            await ctx.GetTable<RaidScoreConfig>()
                .InsertAsync(() => new RaidScoreConfig
                {
                    GuildId = guildId,
                    Enabled = false,
                });
        }
    }
}
