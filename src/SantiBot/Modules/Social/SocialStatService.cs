#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class SocialStatService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public SocialStatService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessage;
        _client.ReactionAdded += OnReaction;
        return Task.CompletedTask;
    }

    private async Task OnMessage(SocketMessage msg)
    {
        try
        {
            if (msg.Author.IsBot || msg.Channel is not ITextChannel tc) return;
            await IncrementStatAsync(tc.GuildId, msg.Author.Id, "messages");
        }
        catch { /* ignore */ }
    }

    private async Task OnReaction(Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> ch, SocketReaction reaction)
    {
        try
        {
            var channel = await ch.GetOrDownloadAsync();
            if (channel is not ITextChannel tc) return;
            if (reaction.UserId == msg.Id) return;

            await IncrementStatAsync(tc.GuildId, reaction.UserId, "reactions");

            // also count "helpful" reactions for the message author
            var message = await msg.GetOrDownloadAsync();
            if (message?.Author is not null && !message.Author.IsBot)
                await IncrementStatAsync(tc.GuildId, message.Author.Id, "helpful");
        }
        catch { /* ignore */ }
    }

    private async Task IncrementStatAsync(ulong guildId, ulong userId, string type)
    {
        await using var ctx = _db.GetDbContext();
        var weekStart = GetWeekStart();

        var existing = await ctx.GetTable<SocialStat>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<SocialStat>()
                .InsertAsync(() => new SocialStat
                {
                    GuildId = guildId,
                    UserId = userId,
                    TotalMessages = type == "messages" ? 1 : 0,
                    TotalReactions = type == "reactions" ? 1 : 0,
                    TotalVoiceMinutes = 0,
                    HelpfulReactions = type == "helpful" ? 1 : 0,
                    WeeklyMessages = type == "messages" ? 1 : 0,
                    WeeklyReactions = type == "reactions" ? 1 : 0,
                    WeeklyVoiceMinutes = 0,
                    WeekStart = weekStart
                });
            return;
        }

        // reset weekly if new week
        if (existing.WeekStart < weekStart)
        {
            await ctx.GetTable<SocialStat>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.WeeklyMessages, 0)
                .Set(x => x.WeeklyReactions, 0)
                .Set(x => x.WeeklyVoiceMinutes, 0)
                .Set(x => x.WeekStart, weekStart)
                .UpdateAsync();
        }

        switch (type)
        {
            case "messages":
                await ctx.GetTable<SocialStat>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.TotalMessages, x => x.TotalMessages + 1)
                    .Set(x => x.WeeklyMessages, x => x.WeeklyMessages + 1)
                    .UpdateAsync();
                break;
            case "reactions":
                await ctx.GetTable<SocialStat>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.TotalReactions, x => x.TotalReactions + 1)
                    .Set(x => x.WeeklyReactions, x => x.WeeklyReactions + 1)
                    .UpdateAsync();
                break;
            case "helpful":
                await ctx.GetTable<SocialStat>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.HelpfulReactions, x => x.HelpfulReactions + 1)
                    .UpdateAsync();
                break;
        }
    }

    public async Task IncrementVoiceAsync(ulong guildId, ulong userId, long minutes)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<SocialStat>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Set(x => x.TotalVoiceMinutes, x => x.TotalVoiceMinutes + minutes)
            .Set(x => x.WeeklyVoiceMinutes, x => x.WeeklyVoiceMinutes + minutes)
            .UpdateAsync();

        if (updated == 0)
        {
            await ctx.GetTable<SocialStat>()
                .InsertAsync(() => new SocialStat
                {
                    GuildId = guildId,
                    UserId = userId,
                    TotalMessages = 0,
                    TotalReactions = 0,
                    TotalVoiceMinutes = minutes,
                    HelpfulReactions = 0,
                    WeeklyMessages = 0,
                    WeeklyReactions = 0,
                    WeeklyVoiceMinutes = minutes,
                    WeekStart = GetWeekStart()
                });
        }
    }

    public async Task<List<SocialStat>> GetLeaderboardAsync(ulong guildId, string type, bool weekly, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        var query = ctx.GetTable<SocialStat>().Where(x => x.GuildId == guildId);

        query = (type, weekly) switch
        {
            ("messages", false) => query.OrderByDescending(x => x.TotalMessages),
            ("messages", true) => query.OrderByDescending(x => x.WeeklyMessages),
            ("reactions", false) => query.OrderByDescending(x => x.TotalReactions),
            ("reactions", true) => query.OrderByDescending(x => x.WeeklyReactions),
            ("voice", false) => query.OrderByDescending(x => x.TotalVoiceMinutes),
            ("voice", true) => query.OrderByDescending(x => x.WeeklyVoiceMinutes),
            ("helpful", _) => query.OrderByDescending(x => x.HelpfulReactions),
            _ => query.OrderByDescending(x => x.TotalMessages)
        };

        return await query.Take(count).ToListAsyncLinqToDB();
    }

    private static DateTime GetWeekStart()
    {
        var now = DateTime.UtcNow.Date;
        var diff = now.DayOfWeek - DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        return now.AddDays(-diff);
    }
}
