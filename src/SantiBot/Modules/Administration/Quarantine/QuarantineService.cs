using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class QuarantineService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    public QuarantineService(DbService db, DiscordSocketClient client, IBotCreds creds)
    {
        _db = db;
        _client = client;
        _creds = creds;
    }

    public Task OnReadyAsync()
    {
        _client.UserJoined += OnUserJoinedAsync;
        return Task.CompletedTask;
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var config = await ctx.GetTable<QuarantineConfig>()
                .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == user.Guild.Id && x.Enabled);

            if (config is null || config.QuarantineRoleId is null)
                return;

            var shouldQuarantine = false;
            var reasons = new List<string>();

            // Check account age
            var accountAge = (DateTime.UtcNow - user.CreatedAt.UtcDateTime).TotalDays;
            if (accountAge < config.MinAccountAgeDays)
            {
                shouldQuarantine = true;
                reasons.Add($"Account age: {accountAge:F1} days (min: {config.MinAccountAgeDays})");
            }

            // Check for default avatar
            if (config.QuarantineNoAvatar && user.GetAvatarUrl() is null)
            {
                shouldQuarantine = true;
                reasons.Add("No custom avatar");
            }

            if (!shouldQuarantine)
                return;

            var role = user.Guild.GetRole(config.QuarantineRoleId.Value);
            if (role is null)
                return;

            await user.AddRoleAsync(role);

            // Log to channel if configured
            if (config.LogChannelId is not null)
            {
                var logChannel = user.Guild.GetTextChannel(config.LogChannelId.Value);
                if (logChannel is not null)
                {
                    var eb = new EmbedBuilder()
                        .WithColor(Color.Orange)
                        .WithTitle("User Quarantined")
                        .WithDescription($"{user.Mention} ({user.Username})")
                        .AddField("Reasons", string.Join("\n", reasons))
                        .AddField("Account Created", user.CreatedAt.ToString("yyyy-MM-dd HH:mm UTC"))
                        .WithCurrentTimestamp();

                    await logChannel.SendMessageAsync(embed: eb.Build());
                }
            }

            Log.Information("Quarantined user {User} in guild {Guild}: {Reasons}",
                user.Username, user.Guild.Name, string.Join(", ", reasons));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error quarantining user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
    }

    public async Task<bool> EnableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<QuarantineConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<QuarantineConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.Enabled, true)
                .UpdateAsync();
        }
        else
        {
            await ctx.GetTable<QuarantineConfig>()
                .InsertAsync(() => new QuarantineConfig
                {
                    GuildId = guildId,
                    Enabled = true,
                });
        }

        return true;
    }

    public async Task<bool> DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<QuarantineConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.Enabled, false)
            .UpdateAsync();
        return updated > 0;
    }

    public async Task<bool> SetRoleAsync(ulong guildId, ulong roleId)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<QuarantineConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<QuarantineConfig>()
                .InsertAsync(() => new QuarantineConfig
                {
                    GuildId = guildId,
                    QuarantineRoleId = roleId,
                });
        }
        else
        {
            await ctx.GetTable<QuarantineConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.QuarantineRoleId, roleId)
                .UpdateAsync();
        }

        return true;
    }

    public async Task<bool> SetMinAgeAsync(ulong guildId, int days)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<QuarantineConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.MinAccountAgeDays, days)
            .UpdateAsync();
        return updated > 0;
    }

    public async Task<bool> SetNoAvatarAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<QuarantineConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.QuarantineNoAvatar, enabled)
            .UpdateAsync();
        return updated > 0;
    }

    public async Task<bool> SetLogChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<QuarantineConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.LogChannelId, channelId)
            .UpdateAsync();
        return updated > 0;
    }

    public async Task<QuarantineConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<QuarantineConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }
}
