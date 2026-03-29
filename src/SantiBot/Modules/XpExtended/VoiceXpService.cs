#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class VoiceXpService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    // track active voice users per guild: (guildId, userId) -> joinTime
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong, ulong), DateTime> _voiceUsers = new();

    public VoiceXpService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.UserVoiceStateUpdated += OnVoiceUpdated;
        _ = Task.Run(VoiceXpLoop);
        return Task.CompletedTask;
    }

    private async Task VoiceXpLoop()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                foreach (var kvp in _voiceUsers)
                {
                    var (guildId, userId) = kvp.Key;
                    var config = await GetConfigAsync(guildId);
                    if (config is null || !config.Enabled) continue;

                    // NOTE: NadekoBot's built-in XpService also awards voice XP.
                    // This VoiceXpService is DISABLED by default (config.Enabled = false).
                    // Admins should only enable it if they've disabled NadekoBot's voice XP
                    // via .xp settings to avoid double-counting.
                    await using var ctx = _db.GetDbContext();
                    await ctx.GetTable<UserXpStats>()
                        .Where(x => x.GuildId == guildId && x.UserId == userId)
                        .Set(x => x.Xp, x => x.Xp + config.XpPerMinute)
                        .UpdateAsync();
                }
            }
            catch { /* ignore */ }
        }
    }

    private Task OnVoiceUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.IsBot) return Task.CompletedTask;

        var guildId = (before.VoiceChannel ?? after.VoiceChannel)?.Guild?.Id;
        if (guildId is null) return Task.CompletedTask;

        var key = (guildId.Value, user.Id);

        if (after.VoiceChannel is null)
        {
            _voiceUsers.TryRemove(key, out _);
        }
        else if (after.IsSelfMuted && after.IsSelfDeafened)
        {
            _voiceUsers.TryRemove(key, out _);
        }
        else
        {
            _voiceUsers[key] = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public async Task<VoiceXpConfig> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<VoiceXpConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task SetEnabledAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<VoiceXpConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<VoiceXpConfig>()
                .InsertAsync(() => new VoiceXpConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                    XpPerMinute = 5
                });
        }
        else
        {
            await ctx.GetTable<VoiceXpConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.Enabled, enabled)
                .UpdateAsync();
        }
    }

    public async Task SetRateAsync(ulong guildId, int xpPerMinute)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<VoiceXpConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<VoiceXpConfig>()
                .InsertAsync(() => new VoiceXpConfig
                {
                    GuildId = guildId,
                    Enabled = true,
                    XpPerMinute = xpPerMinute
                });
        }
        else
        {
            await ctx.GetTable<VoiceXpConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.XpPerMinute, xpPerMinute)
                .UpdateAsync();
        }
    }
}
