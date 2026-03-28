using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class WarnDecayService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    public WarnDecayService(DbService db, DiscordSocketClient client, IBotCreds creds)
    {
        _db = db;
        _client = client;
        _creds = creds;
    }

    public async Task OnReadyAsync()
    {
        // Run once per hour, check for warnings that should decay
        var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await RunDecayAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in warn decay loop");
            }
        }
    }

    private async Task RunDecayAsync()
    {
        await using var ctx = _db.GetDbContext();

        var configs = await ctx.GetTable<WarnDecayConfig>()
            .Where(x => x.Enabled)
            .Where(x => Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var config in configs)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-config.DecayDays);

                // Mark old warnings as decayed (Forgiven column repurposed, or a Decayed flag)
                // Warnings older than DecayDays that haven't already been forgiven get marked decayed
                var updated = await ctx.GetTable<Warning>()
                    .Where(w => w.GuildId == config.GuildId)
                    .Where(w => !w.Forgiven)
                    .Where(w => w.DateAdded < cutoff)
                    .Set(w => w.Forgiven, true)
                    .Set(w => w.ForgivenBy, "WarnDecay")
                    .UpdateAsync();

                if (updated > 0)
                {
                    Log.Information("Warn decay: decayed {Count} warnings in guild {GuildId}",
                        updated, config.GuildId);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error decaying warns for guild {GuildId}", config.GuildId);
            }
        }
    }

    public async Task<bool> EnableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<WarnDecayConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<WarnDecayConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.Enabled, true)
                .UpdateAsync();
        }
        else
        {
            await ctx.GetTable<WarnDecayConfig>()
                .InsertAsync(() => new WarnDecayConfig
                {
                    GuildId = guildId,
                    Enabled = true,
                    DecayDays = 30,
                    MinWarnsToDecay = 1,
                });
        }

        return true;
    }

    public async Task<bool> DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<WarnDecayConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.Enabled, false)
            .UpdateAsync();
        return updated > 0;
    }

    public async Task<bool> SetDecayDaysAsync(ulong guildId, int days)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<WarnDecayConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.DecayDays, days)
            .UpdateAsync();
        return updated > 0;
    }

    public async Task<WarnDecayConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<WarnDecayConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }
}
