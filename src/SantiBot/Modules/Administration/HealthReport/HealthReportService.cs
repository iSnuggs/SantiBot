#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class HealthReportService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private Timer _timer;

    public HealthReportService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        // Check every 24 hours for weekly report
        _timer = new Timer(async _ => await CheckAndSendReportsAsync(), null, TimeSpan.FromMinutes(10), TimeSpan.FromHours(24));
        return Task.CompletedTask;
    }

    private async Task CheckAndSendReportsAsync()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var configs = await ctx.GetTable<HealthReportConfig>()
                .Where(x => x.AutoEnabled)
                .ToListAsyncLinqToDB();

            foreach (var config in configs)
            {
                try
                {
                    // Send weekly only
                    if (config.LastReportDate.HasValue && (DateTime.UtcNow - config.LastReportDate.Value).TotalDays < 7)
                        continue;

                    var guild = _client.GetGuild(config.GuildId);
                    if (guild is null) continue;

                    var report = await GenerateReportAsync(guild);

                    var owner = guild.GetUser(guild.OwnerId);
                    if (owner is null) continue;

                    try
                    {
                        var dm = await owner.CreateDMChannelAsync();
                        await dm.SendMessageAsync(embed: report.Build());
                    }
                    catch { }

                    await ctx.GetTable<HealthReportConfig>()
                        .Where(x => x.Id == config.Id)
                        .UpdateAsync(x => new HealthReportConfig { LastReportDate = DateTime.UtcNow });
                }
                catch { }
            }
        }
        catch { }
    }

    public async Task<EmbedBuilder> GenerateReportAsync(IGuild guild)
    {
        var socketGuild = guild as SocketGuild ?? _client.GetGuild(guild.Id);
        if (socketGuild is null) return new EmbedBuilder().WithDescription("Guild not found.");

        var embed = new EmbedBuilder()
            .WithTitle($"Server Health Report: {guild.Name}")
            .WithColor(Color.Green)
            .WithTimestamp(DateTimeOffset.UtcNow);

        // Member stats
        var totalMembers = socketGuild.MemberCount;
        var onlineMembers = socketGuild.Users.Count(u => u.Status != UserStatus.Offline);
        embed.AddField("Members", $"Total: **{totalMembers}**\nOnline: **{onlineMembers}**\nBots: **{socketGuild.Users.Count(u => u.IsBot)}**", true);

        // Channel stats
        var textChannels = socketGuild.TextChannels.Count;
        var voiceChannels = socketGuild.VoiceChannels.Count;
        embed.AddField("Channels", $"Text: **{textChannels}**\nVoice: **{voiceChannels}**\nCategories: **{socketGuild.CategoryChannels.Count}**", true);

        // Role stats
        embed.AddField("Roles", $"Total: **{socketGuild.Roles.Count}**", true);

        // Activity data from our tracking
        try
        {
            await using var ctx = _db.GetDbContext();
            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-7);
            var activity = await ctx.GetTable<ChannelActivity>()
                .Where(x => x.GuildId == guild.Id && x.TrackedDate >= sevenDaysAgo)
                .ToListAsyncLinqToDB();

            if (activity.Count > 0)
            {
                var totalMessages = activity.Sum(x => x.MessageCount);
                var mostActive = activity
                    .GroupBy(x => x.ChannelId)
                    .OrderByDescending(g => g.Sum(x => x.MessageCount))
                    .Take(3)
                    .Select(g => $"<#{g.Key}>: **{g.Sum(x => x.MessageCount)}**");

                embed.AddField("Messages (7 days)", $"Total: **{totalMessages:N0}**\nTop channels:\n{string.Join("\n", mostActive)}");

                var leastActive = activity
                    .GroupBy(x => x.ChannelId)
                    .OrderBy(g => g.Sum(x => x.MessageCount))
                    .Take(3)
                    .Select(g => $"<#{g.Key}>: **{g.Sum(x => x.MessageCount)}**");

                embed.AddField("Least Active Channels", string.Join("\n", leastActive));
            }
            else
            {
                embed.AddField("Messages", "No activity data yet.");
            }

            // Mod actions (warnings) in the past week
            var recentWarnings = await ctx.GetTable<WarningPoint>()
                .Where(x => x.GuildId == guild.Id && x.DateAdded >= sevenDaysAgo)
                .CountAsyncLinqToDB();

            var recentNotes = await ctx.GetTable<UserNote>()
                .Where(x => x.GuildId == guild.Id && x.DateAdded >= sevenDaysAgo)
                .CountAsyncLinqToDB();

            embed.AddField("Mod Actions (7 days)", $"Warning Points: **{recentWarnings}**\nUser Notes: **{recentNotes}**", true);
        }
        catch
        {
            embed.AddField("Activity Data", "Could not load activity data.");
        }

        return embed;
    }

    public async Task ToggleAutoAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<HealthReportConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<HealthReportConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new HealthReportConfig { AutoEnabled = enabled });
        }
        else
        {
            await ctx.GetTable<HealthReportConfig>()
                .InsertAsync(() => new HealthReportConfig
                {
                    GuildId = guildId,
                    AutoEnabled = enabled
                });
        }
    }
}
