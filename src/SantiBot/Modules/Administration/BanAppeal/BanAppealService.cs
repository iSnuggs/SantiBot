using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class BanAppealService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public BanAppealService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<bool> EnableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<BanAppealConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<BanAppealConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.Enabled, true)
                .UpdateAsync();
        }
        else
        {
            await ctx.GetTable<BanAppealConfig>()
                .InsertAsync(() => new BanAppealConfig
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
        var updated = await ctx.GetTable<BanAppealConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.Enabled, false)
            .UpdateAsync();
        return updated > 0;
    }

    public async Task<bool> SetReviewChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<BanAppealConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<BanAppealConfig>()
                .InsertAsync(() => new BanAppealConfig
                {
                    GuildId = guildId,
                    ReviewChannelId = channelId,
                });
        }
        else
        {
            await ctx.GetTable<BanAppealConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.ReviewChannelId, channelId)
                .UpdateAsync();
        }

        return true;
    }

    public async Task<bool> SetAppealMessageAsync(ulong guildId, string message)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<BanAppealConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.AppealMessage, message)
            .UpdateAsync();
        return updated > 0;
    }

    public async Task<BanAppealConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<BanAppealConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task<BanAppeal> SubmitAppealAsync(ulong guildId, ulong userId, string reason, string appealText)
    {
        await using var ctx = _db.GetDbContext();

        var appeal = await ctx.GetTable<BanAppeal>()
            .InsertWithOutputAsync(() => new BanAppeal
            {
                GuildId = guildId,
                UserId = userId,
                Reason = reason,
                AppealText = appealText,
                SubmittedAt = DateTime.UtcNow,
                Status = "Pending",
            });

        // Try to post to review channel
        try
        {
            var config = await ctx.GetTable<BanAppealConfig>()
                .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Enabled);

            if (config?.ReviewChannelId is not null)
            {
                var guild = _client.GetGuild(guildId);
                var channel = guild?.GetTextChannel(config.ReviewChannelId.Value);
                if (channel is not null)
                {
                    var user = await _client.GetUserAsync(userId);
                    var eb = new EmbedBuilder()
                        .WithColor(Color.Gold)
                        .WithTitle($"Ban Appeal #{appeal.Id}")
                        .WithDescription(appealText)
                        .AddField("User", user is not null ? $"{user.Username} ({userId})" : userId.ToString())
                        .AddField("Original Ban Reason", string.IsNullOrEmpty(reason) ? "No reason provided" : reason)
                        .AddField("Status", "Pending")
                        .WithCurrentTimestamp();

                    await channel.SendMessageAsync(embed: eb.Build());
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error posting ban appeal to review channel");
        }

        return appeal;
    }

    public async Task<bool> ReviewAppealAsync(ulong guildId, int appealId, string status, ulong reviewerId)
    {
        await using var ctx = _db.GetDbContext();

        var updated = await ctx.GetTable<BanAppeal>()
            .Where(x => x.Id == appealId && x.GuildId == guildId && x.Status == "Pending")
            .Set(x => x.Status, status)
            .Set(x => x.ReviewedByUserId, reviewerId)
            .UpdateAsync();

        if (updated == 0)
            return false;

        // If approved, try to unban the user
        if (status == "Approved")
        {
            try
            {
                var appeal = await ctx.GetTable<BanAppeal>()
                    .FirstOrDefaultAsyncLinqToDB(x => x.Id == appealId);

                if (appeal is not null)
                {
                    var guild = _client.GetGuild(guildId);
                    if (guild is not null)
                    {
                        await guild.RemoveBanAsync(appeal.UserId,
                            new RequestOptions { AuditLogReason = $"Ban appeal #{appealId} approved" });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error unbanning user from approved appeal {AppealId}", appealId);
            }
        }

        return true;
    }

    public async Task<List<BanAppeal>> ListPendingAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<BanAppeal>()
            .Where(x => x.GuildId == guildId && x.Status == "Pending")
            .OrderBy(x => x.SubmittedAt)
            .ToListAsyncLinqToDB();
    }
}
