#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class AutoArchiveService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private Timer _timer;

    public AutoArchiveService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        // Check every 6 hours
        _timer = new Timer(async _ => await CheckArchiveAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(6));
        return Task.CompletedTask;
    }

    private async Task CheckArchiveAsync()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var configs = await ctx.GetTable<AutoArchiveConfig>()
                .Where(x => x.IsEnabled)
                .ToListAsyncLinqToDB();

            foreach (var config in configs)
            {
                try
                {
                    var guild = _client.GetGuild(config.GuildId);
                    if (guild is null) continue;

                    var exclusions = await ctx.GetTable<AutoArchiveExclusion>()
                        .Where(x => x.GuildId == config.GuildId)
                        .Select(x => x.ChannelId)
                        .ToListAsyncLinqToDB();

                    var excludedSet = exclusions.ToHashSet();
                    var cutoff = DateTimeOffset.UtcNow.AddDays(-config.InactiveDays);

                    foreach (var channel in guild.TextChannels)
                    {
                        if (excludedSet.Contains(channel.Id)) continue;
                        if (channel.CategoryId is not null) // Skip if already in archive-like category
                        {
                            var category = guild.GetCategoryChannel(channel.CategoryId.Value);
                            if (category?.Name.Contains("archive", StringComparison.OrdinalIgnoreCase) == true)
                                continue;
                        }

                        try
                        {
                            var messages = await channel.GetMessagesAsync(1).FlattenAsync();
                            var lastMsg = messages.FirstOrDefault();

                            if (lastMsg is null || lastMsg.Timestamp < cutoff)
                            {
                                // Archive by adding "archived-" prefix and making read-only
                                if (!channel.Name.StartsWith("archived-"))
                                {
                                    var everyoneRole = guild.EveryoneRole;
                                    await channel.AddPermissionOverwriteAsync(everyoneRole,
                                        new OverwritePermissions(sendMessages: PermValue.Deny));
                                    await channel.ModifyAsync(c => c.Name = $"archived-{channel.Name}");
                                }
                            }
                        }
                        catch { /* skip channels we can't read */ }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    public async Task SetConfigAsync(ulong guildId, int days)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<AutoArchiveConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<AutoArchiveConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new AutoArchiveConfig { InactiveDays = days, IsEnabled = true });
        }
        else
        {
            await ctx.GetTable<AutoArchiveConfig>()
                .InsertAsync(() => new AutoArchiveConfig
                {
                    GuildId = guildId,
                    InactiveDays = days,
                    IsEnabled = true
                });
        }
    }

    public async Task DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<AutoArchiveConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new AutoArchiveConfig { IsEnabled = false });
    }

    public async Task AddExclusionAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var exists = await ctx.GetTable<AutoArchiveExclusion>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.ChannelId == channelId);

        if (!exists)
        {
            await ctx.GetTable<AutoArchiveExclusion>()
                .InsertAsync(() => new AutoArchiveExclusion { GuildId = guildId, ChannelId = channelId });
        }
    }

    public async Task<(AutoArchiveConfig Config, List<AutoArchiveExclusion> Exclusions)> GetInfoAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<AutoArchiveConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        var exclusions = await ctx.GetTable<AutoArchiveExclusion>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return (config, exclusions);
    }
}
