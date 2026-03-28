using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class AltDetectService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public AltDetectService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
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

            var accountAge = DateTimeOffset.UtcNow - user.CreatedAt;

            // Check if account is under minimum age
            if (accountAge.TotalDays >= config.MinAccountAgeDays)
                return;

            // Check if this user is already a known alt
            var existingAlt = await GetAltEntryAsync(user.Guild.Id, user.Id);

            await TakeActionAsync(user, config, accountAge, existingAlt);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in AltDetect for guild {GuildId}", user.Guild.Id);
        }
    }

    private async Task TakeActionAsync(SocketGuildUser user, AltDetectConfig config, TimeSpan accountAge, KnownAlt? existing)
    {
        var action = config.Action?.ToLowerInvariant() ?? "alert";

        switch (action)
        {
            case "ban":
                try { await user.BanAsync(reason: $"Alt detection: account age {accountAge.TotalHours:F1}h < {config.MinAccountAgeDays}d minimum"); }
                catch { /* Missing permissions */ }
                break;

            case "kick":
                try { await user.KickAsync($"Alt detection: account age {accountAge.TotalHours:F1}h < {config.MinAccountAgeDays}d minimum"); }
                catch { /* Missing permissions */ }
                break;
        }

        // Always alert
        if (config.AlertChannelId is { } channelId
            && user.Guild.GetTextChannel(channelId) is { } alertCh)
        {
            var desc = $"{user.Mention} ({user.Username}) joined with an account only **{accountAge.TotalHours:F1} hours** old "
                     + $"(minimum: {config.MinAccountAgeDays} days).";

            if (existing is not null)
                desc += $"\n\nThis user is a **known alt** of <@{existing.MainUserId}>. Reason: {existing.Reason}";

            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Alt Account Detected")
                .WithDescription(desc)
                .AddField("Action Taken", action, true)
                .AddField("Account Created", user.CreatedAt.ToString("yyyy-MM-dd HH:mm UTC"), true)
                .WithCurrentTimestamp()
                .Build();

            try { await alertCh.SendMessageAsync(embed: embed); }
            catch { /* Missing permissions */ }
        }
    }

    // ── Config CRUD ──

    public async Task<AltDetectConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<AltDetectConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<AltDetectConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<AltDetectConfig>()
                .InsertAsync(() => new AltDetectConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                });
        }
        else
        {
            await ctx.GetTable<AltDetectConfig>()
                .Where(x => x.GuildId == guildId)
                .UpdateAsync(x => new AltDetectConfig { Enabled = enabled });
        }
    }

    public async Task SetMinAgeAsync(ulong guildId, int days)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<AltDetectConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new AltDetectConfig { MinAccountAgeDays = days });
    }

    public async Task SetActionAsync(ulong guildId, string action)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<AltDetectConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new AltDetectConfig { Action = action });
    }

    public async Task SetAlertChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<AltDetectConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new AltDetectConfig { AlertChannelId = channelId });
    }

    public async Task MarkAltAsync(ulong guildId, ulong mainUserId, ulong altUserId, string reason)
    {
        await using var ctx = _db.GetDbContext();

        // Remove existing link if any
        await ctx.GetTable<KnownAlt>()
            .DeleteAsync(x => x.GuildId == guildId && x.AltUserId == altUserId);

        await ctx.GetTable<KnownAlt>()
            .InsertAsync(() => new KnownAlt
            {
                GuildId = guildId,
                MainUserId = mainUserId,
                AltUserId = altUserId,
                Reason = reason,
                DetectedAt = DateTime.UtcNow,
            });
    }

    public async Task<List<KnownAlt>> GetAltsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<KnownAlt>()
            .Where(x => x.GuildId == guildId && (x.MainUserId == userId || x.AltUserId == userId))
            .ToListAsyncLinqToDB();
    }

    private async Task<KnownAlt?> GetAltEntryAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<KnownAlt>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.AltUserId == userId);
    }

    private async Task EnsureConfigAsync(SantiContext ctx, ulong guildId)
    {
        var exists = await ctx.GetTable<AltDetectConfig>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId);

        if (!exists)
        {
            await ctx.GetTable<AltDetectConfig>()
                .InsertAsync(() => new AltDetectConfig
                {
                    GuildId = guildId,
                    Enabled = false,
                });
        }
    }
}
