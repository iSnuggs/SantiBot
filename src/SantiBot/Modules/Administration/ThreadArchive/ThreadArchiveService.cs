using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class ThreadArchiveService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    public ThreadArchiveService(
        DbService db,
        DiscordSocketClient client,
        IBotCreds creds)
    {
        _db = db;
        _client = client;
        _creds = creds;
    }

    public async Task OnReadyAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));

        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await KeepAliveThreadsAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in thread keep-alive loop");
            }
        }
    }

    private async Task KeepAliveThreadsAsync()
    {
        await using var ctx = _db.GetDbContext();

        var keepAliveConfigs = await ctx.GetTable<ThreadArchiveConfig>()
            .Where(x => x.KeepAlive
                         && Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var config in keepAliveConfigs)
        {
            try
            {
                var guild = _client.GetGuild(config.GuildId);
                if (guild is null)
                    continue;

                var channel = guild.GetChannel(config.ChannelId);
                if (channel is not SocketTextChannel textChannel)
                    continue;

                foreach (var thread in textChannel.Threads)
                {
                    if (thread.IsArchived)
                    {
                        try
                        {
                            await thread.ModifyAsync(t => t.Archived = false);
                        }
                        catch
                        {
                            // Missing perms or thread deleted
                        }
                    }
                }
            }
            catch
            {
                // Guild/channel no longer accessible
            }
        }
    }

    public async Task SetArchiveTimeAsync(ulong guildId, ulong channelId, int minutes)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<ThreadArchiveConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.ChannelId == channelId);

        if (existing is null)
        {
            await ctx.GetTable<ThreadArchiveConfig>()
                .InsertAsync(() => new ThreadArchiveConfig
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    ArchiveAfterMinutes = minutes,
                    KeepAlive = false,
                });
        }
        else
        {
            await ctx.GetTable<ThreadArchiveConfig>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.ArchiveAfterMinutes, minutes)
                .Set(x => x.KeepAlive, false)
                .UpdateAsync();
        }
    }

    public async Task SetKeepAliveAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<ThreadArchiveConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.ChannelId == channelId);

        if (existing is null)
        {
            await ctx.GetTable<ThreadArchiveConfig>()
                .InsertAsync(() => new ThreadArchiveConfig
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    KeepAlive = true,
                });
        }
        else
        {
            await ctx.GetTable<ThreadArchiveConfig>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.KeepAlive, true)
                .UpdateAsync();
        }
    }

    public async Task<bool> RemoveConfigAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var deleted = await ctx.GetTable<ThreadArchiveConfig>()
            .Where(x => x.GuildId == guildId && x.ChannelId == channelId)
            .DeleteAsync();

        return deleted > 0;
    }

    public async Task<IReadOnlyList<ThreadArchiveConfig>> GetConfigsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<ThreadArchiveConfig>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
