using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class AutoPurgeService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    public AutoPurgeService(DbService db, DiscordSocketClient client, IBotCreds creds)
    {
        _db = db;
        _client = client;
        _creds = creds;
    }

    public async Task OnReadyAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await RunPurgesAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in auto purge loop");
            }
        }
    }

    private async Task RunPurgesAsync()
    {
        await using var ctx = _db.GetDbContext();
        var configs = await ctx.GetTable<AutoPurgeConfig>()
            .Where(x => x.IsActive)
            .Where(x => Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var config in configs)
        {
            try
            {
                var guild = _client.GetGuild(config.GuildId);
                var channel = guild?.GetTextChannel(config.ChannelId);
                if (channel is null)
                    continue;

                // Check if enough time has passed since last run
                var cacheKey = $"autopurge:{config.Id}";
                var now = DateTime.UtcNow;
                var cutoff = now.AddHours(-config.MaxMessageAgeHours);

                // Discord API limitation: can only bulk delete messages less than 14 days old
                var minDate = now.AddDays(-14);
                if (cutoff < minDate)
                    cutoff = minDate;

                var messages = (await channel.GetMessagesAsync(100).FlattenAsync())
                    .Where(m => m.Timestamp.UtcDateTime < cutoff && !m.IsPinned)
                    .ToList();

                if (messages.Count == 0)
                    continue;

                // Bulk delete (only works for messages < 14 days old)
                var bulkDeletable = messages.Where(m => (now - m.Timestamp.UtcDateTime).TotalDays < 14).ToList();
                if (bulkDeletable.Count > 0)
                {
                    await channel.DeleteMessagesAsync(bulkDeletable);
                    Log.Information("Auto-purged {Count} messages from {Channel} in {Guild}",
                        bulkDeletable.Count, channel.Name, guild?.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error auto-purging channel {ChannelId} in guild {GuildId}",
                    config.ChannelId, config.GuildId);
            }
        }
    }

    public async Task<AutoPurgeConfig?> SetAutoPurgeAsync(ulong guildId, ulong channelId, int intervalHours, int maxAgeHours)
    {
        await using var ctx = _db.GetDbContext();

        // Remove existing config for this channel
        await ctx.GetTable<AutoPurgeConfig>()
            .DeleteAsync(x => x.GuildId == guildId && x.ChannelId == channelId);

        return await ctx.GetTable<AutoPurgeConfig>()
            .InsertWithOutputAsync(() => new AutoPurgeConfig
            {
                GuildId = guildId,
                ChannelId = channelId,
                IntervalHours = intervalHours,
                MaxMessageAgeHours = maxAgeHours,
                IsActive = true,
            });
    }

    public async Task<bool> RemoveAutoPurgeAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<AutoPurgeConfig>()
            .DeleteAsync(x => x.GuildId == guildId && x.ChannelId == channelId);
        return deleted > 0;
    }

    public async Task<List<AutoPurgeConfig>> GetAutoPurgesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<AutoPurgeConfig>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .ToListAsyncLinqToDB();
    }
}
