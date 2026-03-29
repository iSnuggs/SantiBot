#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class BanSyncService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public BanSyncService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<bool> AddLinkAsync(ulong guildId, ulong linkedGuildId)
    {
        await using var ctx = _db.GetDbContext();
        var exists = await ctx.GetTable<BanSyncConfig>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.LinkedGuildId == linkedGuildId);

        if (exists) return false;

        await ctx.GetTable<BanSyncConfig>()
            .InsertAsync(() => new BanSyncConfig { GuildId = guildId, LinkedGuildId = linkedGuildId });
        return true;
    }

    public async Task<bool> RemoveLinkAsync(ulong guildId, ulong linkedGuildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<BanSyncConfig>()
            .Where(x => x.GuildId == guildId && x.LinkedGuildId == linkedGuildId)
            .DeleteAsync() > 0;
    }

    public async Task<List<BanSyncConfig>> ListLinksAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<BanSyncConfig>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<int> PushBansAsync(ulong guildId, IGuild sourceGuild)
    {
        await using var ctx = _db.GetDbContext();
        var links = await ctx.GetTable<BanSyncConfig>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        var bans = await sourceGuild.GetBansAsync(limit: 1000).FlattenAsync();
        var count = 0;

        foreach (var link in links)
        {
            var targetGuild = _client.GetGuild(link.LinkedGuildId);
            if (targetGuild is null) continue;

            foreach (var ban in bans)
            {
                try
                {
                    await targetGuild.AddBanAsync(ban.User.Id, reason: $"Ban sync from {sourceGuild.Name}: {ban.Reason}");
                    count++;
                }
                catch { /* skip if already banned or no perms */ }
            }
        }

        return count;
    }

    public async Task<int> PullBansAsync(ulong guildId, IGuild targetGuild)
    {
        await using var ctx = _db.GetDbContext();
        var links = await ctx.GetTable<BanSyncConfig>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        var count = 0;

        foreach (var link in links)
        {
            var sourceGuild = _client.GetGuild(link.LinkedGuildId);
            if (sourceGuild is null) continue;

            var bans = await sourceGuild.GetBansAsync(limit: 1000).FlattenAsync();
            foreach (var ban in bans)
            {
                try
                {
                    await targetGuild.AddBanAsync(ban.User.Id, reason: $"Ban sync from {sourceGuild.Name}: {ban.Reason}");
                    count++;
                }
                catch { }
            }
        }

        return count;
    }
}
