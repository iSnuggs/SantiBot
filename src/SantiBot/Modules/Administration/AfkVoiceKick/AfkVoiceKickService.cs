using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class AfkVoiceKickService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    // Track last voice activity per user: (guildId, userId) -> last active time
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong GuildId, ulong UserId), DateTimeOffset> _lastActivity = new();

    public AfkVoiceKickService(DbService db, DiscordSocketClient client, IBotCreds creds)
    {
        _db = db;
        _client = client;
        _creds = creds;
    }

    public async Task OnReadyAsync()
    {
        _client.UserVoiceStateUpdated += OnVoiceStateUpdated;

        // Background loop: check for idle users every 60 seconds
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await CheckIdleUsersAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in AFK voice kick loop");
            }
        }
    }

    private Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        // Any voice state change counts as activity
        if (after.VoiceChannel is not null)
        {
            var guildId = after.VoiceChannel.Guild.Id;
            _lastActivity[(guildId, user.Id)] = DateTimeOffset.UtcNow;
        }

        // User left voice entirely: clean up tracking
        if (after.VoiceChannel is null && before.VoiceChannel is not null)
        {
            var guildId = before.VoiceChannel.Guild.Id;
            _lastActivity.TryRemove((guildId, user.Id), out _);
        }

        return Task.CompletedTask;
    }

    private async Task CheckIdleUsersAsync()
    {
        await using var ctx = _db.GetDbContext();
        var configs = await ctx.GetTable<AfkVoiceKickConfig>()
            .Where(x => x.Enabled)
            .Where(x => Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToListAsyncLinqToDB();

        var now = DateTimeOffset.UtcNow;

        foreach (var config in configs)
        {
            try
            {
                var guild = _client.GetGuild(config.GuildId);
                if (guild is null)
                    continue;

                // Check all users in voice channels
                foreach (var voiceCh in guild.VoiceChannels)
                {
                    foreach (var user in voiceCh.ConnectedUsers)
                    {
                        if (user.IsBot)
                            continue;

                        // Check exempt role
                        if (config.ExemptRoleId is { } exemptId && user.Roles.Any(r => r.Id == exemptId))
                            continue;

                        // Get or initialize last activity
                        var key = (config.GuildId, user.Id);
                        var lastActive = _lastActivity.GetOrAdd(key, now);

                        var idleMinutes = (now - lastActive).TotalMinutes;
                        if (idleMinutes < config.IdleMinutes)
                            continue;

                        // User is idle: move or disconnect
                        if (config.AfkChannelId is { } afkChId
                            && guild.GetVoiceChannel(afkChId) is { } afkCh
                            && voiceCh.Id != afkChId) // don't move if already in AFK channel
                        {
                            try { await user.ModifyAsync(u => u.Channel = afkCh); }
                            catch { /* Missing permissions */ }
                        }
                        else if (config.AfkChannelId is null)
                        {
                            try { await user.ModifyAsync(u => u.Channel = null); }
                            catch { /* Missing permissions */ }
                        }

                        // Reset activity so they don't get immediately kicked again
                        _lastActivity[key] = now;

                        Log.Information("AFK voice action on {User} in {Guild}: idle {Minutes:F0}min",
                            user.Username, guild.Name, idleMinutes);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error checking idle users in guild {GuildId}", config.GuildId);
            }
        }
    }

    // ── Config CRUD ──

    public async Task<AfkVoiceKickConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<AfkVoiceKickConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<AfkVoiceKickConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<AfkVoiceKickConfig>()
                .InsertAsync(() => new AfkVoiceKickConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                });
        }
        else
        {
            await ctx.GetTable<AfkVoiceKickConfig>()
                .Where(x => x.GuildId == guildId)
                .UpdateAsync(x => new AfkVoiceKickConfig { Enabled = enabled });
        }
    }

    public async Task SetIdleMinutesAsync(ulong guildId, int minutes)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<AfkVoiceKickConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new AfkVoiceKickConfig { IdleMinutes = minutes });
    }

    public async Task SetExemptRoleAsync(ulong guildId, ulong roleId)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<AfkVoiceKickConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new AfkVoiceKickConfig { ExemptRoleId = roleId });
    }

    public async Task SetAfkChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<AfkVoiceKickConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new AfkVoiceKickConfig { AfkChannelId = channelId });
    }

    private async Task EnsureConfigAsync(SantiContext ctx, ulong guildId)
    {
        var exists = await ctx.GetTable<AfkVoiceKickConfig>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId);

        if (!exists)
        {
            await ctx.GetTable<AfkVoiceKickConfig>()
                .InsertAsync(() => new AfkVoiceKickConfig
                {
                    GuildId = guildId,
                    Enabled = false,
                });
        }
    }
}
