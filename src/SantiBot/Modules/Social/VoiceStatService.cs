#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class VoiceStatService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly SocialStatService _socialStats;
    private readonly AchievementService _achievements;

    // track who is in voice and when they joined: (guildId, userId) -> (joinTime, channelId)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong, ulong), (DateTime JoinTime, ulong ChannelId)> _voiceSessions = new();

    public VoiceStatService(DbService db, DiscordSocketClient client,
        SocialStatService socialStats, AchievementService achievements)
    {
        _db = db;
        _client = client;
        _socialStats = socialStats;
        _achievements = achievements;
    }

    public Task OnReadyAsync()
    {
        _client.UserVoiceStateUpdated += OnVoiceStateUpdated;
        return Task.CompletedTask;
    }

    private async Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        try
        {
            if (user.IsBot) return;

            var guildId = (before.VoiceChannel ?? after.VoiceChannel)?.Guild?.Id;
            if (guildId is null) return;

            var key = (guildId.Value, user.Id);

            // user left voice
            if (before.VoiceChannel is not null && after.VoiceChannel is null)
            {
                if (_voiceSessions.TryRemove(key, out var session))
                {
                    var minutes = (long)(DateTime.UtcNow - session.JoinTime).TotalMinutes;
                    if (minutes > 0)
                    {
                        await RecordVoiceTimeAsync(guildId.Value, user.Id, minutes, session.ChannelId);
                        await _socialStats.IncrementVoiceAsync(guildId.Value, user.Id, minutes);

                        // check voice achievements
                        await CheckVoiceAchievementsAsync(guildId.Value, user.Id);

                        // record voice partners
                        await RecordVoicePartnersAsync(guildId.Value, user.Id, before.VoiceChannel, minutes);
                    }
                }
            }
            // user joined voice
            else if (before.VoiceChannel is null && after.VoiceChannel is not null)
            {
                // skip if muted and deafened (AFK)
                if (!after.IsSelfMuted || !after.IsSelfDeafened)
                    _voiceSessions[key] = (DateTime.UtcNow, after.VoiceChannel.Id);
            }
            // user moved channels
            else if (before.VoiceChannel is not null && after.VoiceChannel is not null
                     && before.VoiceChannel.Id != after.VoiceChannel.Id)
            {
                if (_voiceSessions.TryRemove(key, out var session))
                {
                    var minutes = (long)(DateTime.UtcNow - session.JoinTime).TotalMinutes;
                    if (minutes > 0)
                    {
                        await RecordVoiceTimeAsync(guildId.Value, user.Id, minutes, session.ChannelId);
                        await _socialStats.IncrementVoiceAsync(guildId.Value, user.Id, minutes);
                    }
                }
                if (!after.IsSelfMuted || !after.IsSelfDeafened)
                    _voiceSessions[key] = (DateTime.UtcNow, after.VoiceChannel.Id);
            }
            // user muted/deafened (went AFK)
            else if (after.IsSelfMuted && after.IsSelfDeafened && _voiceSessions.ContainsKey(key))
            {
                if (_voiceSessions.TryRemove(key, out var session))
                {
                    var minutes = (long)(DateTime.UtcNow - session.JoinTime).TotalMinutes;
                    if (minutes > 0)
                    {
                        await RecordVoiceTimeAsync(guildId.Value, user.Id, minutes, session.ChannelId);
                        await _socialStats.IncrementVoiceAsync(guildId.Value, user.Id, minutes);
                    }
                }
            }
            // user unmuted
            else if ((!after.IsSelfMuted || !after.IsSelfDeafened) && !_voiceSessions.ContainsKey(key)
                     && after.VoiceChannel is not null)
            {
                _voiceSessions[key] = (DateTime.UtcNow, after.VoiceChannel.Id);
            }
        }
        catch { /* ignore */ }
    }

    private async Task RecordVoiceTimeAsync(ulong guildId, ulong userId, long minutes, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<VoiceStat>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<VoiceStat>()
                .InsertAsync(() => new VoiceStat
                {
                    GuildId = guildId,
                    UserId = userId,
                    TotalMinutes = minutes,
                    FavoriteChannelId = channelId,
                    FavoriteChannelMinutes = minutes
                });
        }
        else
        {
            await ctx.GetTable<VoiceStat>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.TotalMinutes, x => x.TotalMinutes + minutes)
                .UpdateAsync();

            // update favorite channel if this one surpasses
            if (channelId == existing.FavoriteChannelId)
            {
                await ctx.GetTable<VoiceStat>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.FavoriteChannelMinutes, x => x.FavoriteChannelMinutes + minutes)
                    .UpdateAsync();
            }
            else if (minutes > existing.FavoriteChannelMinutes)
            {
                await ctx.GetTable<VoiceStat>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.FavoriteChannelId, channelId)
                    .Set(x => x.FavoriteChannelMinutes, minutes)
                    .UpdateAsync();
            }
        }
    }

    private async Task RecordVoicePartnersAsync(ulong guildId, ulong userId, SocketVoiceChannel channel, long minutes)
    {
        if (channel is null) return;
        foreach (var other in channel.ConnectedUsers)
        {
            if (other.Id == userId || other.IsBot) continue;

            var u1 = Math.Min(userId, other.Id);
            var u2 = Math.Max(userId, other.Id);

            await using var ctx = _db.GetDbContext();
            var updated = await ctx.GetTable<VoicePartner>()
                .Where(x => x.GuildId == guildId && x.User1Id == u1 && x.User2Id == u2)
                .Set(x => x.SharedMinutes, x => x.SharedMinutes + minutes)
                .UpdateAsync();

            if (updated == 0)
            {
                await ctx.GetTable<VoicePartner>()
                    .InsertAsync(() => new VoicePartner
                    {
                        GuildId = guildId,
                        User1Id = u1,
                        User2Id = u2,
                        SharedMinutes = minutes
                    });
            }
        }
    }

    private async Task CheckVoiceAchievementsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var stat = await ctx.GetTable<VoiceStat>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (stat is null) return;
        if (stat.TotalMinutes >= 60)
            await _achievements.TryAwardAsync(guildId, userId, "voice_1h");
        if (stat.TotalMinutes >= 1440)
            await _achievements.TryAwardAsync(guildId, userId, "voice_24h");
    }

    public async Task<VoiceStat> GetVoiceStatsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<VoiceStat>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task<List<VoiceStat>> GetVoiceLeaderboardAsync(ulong guildId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<VoiceStat>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.TotalMinutes)
            .Take(count)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<VoicePartner>> GetTopPartnersAsync(ulong guildId, ulong userId, int count = 5)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<VoicePartner>()
            .Where(x => x.GuildId == guildId && (x.User1Id == userId || x.User2Id == userId))
            .OrderByDescending(x => x.SharedMinutes)
            .Take(count)
            .ToListAsyncLinqToDB();
    }
}
