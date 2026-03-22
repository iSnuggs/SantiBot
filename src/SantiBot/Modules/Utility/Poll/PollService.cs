using System.Text.Json;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class PollService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IBotCreds _creds;

    public PollService(DbService db, DiscordSocketClient client, IMessageSenderService sender, IBotCreds creds)
    {
        _db = db;
        _client = client;
        _sender = sender;
        _creds = creds;
    }

    public async Task OnReadyAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await using var ctx = _db.GetDbContext();
                var expired = await ctx.GetTable<PollModel>()
                    .Where(x => x.IsActive && x.EndsAt != null && x.EndsAt <= DateTime.UtcNow)
                    .Where(x => Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
                    .ToListAsyncLinqToDB();

                foreach (var poll in expired)
                {
                    try
                    {
                        await EndPollInternalAsync(poll);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to end poll {PollId}", poll.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in poll timer loop");
            }
        }
    }

    public async Task<PollModel?> CreatePollAsync(
        ulong guildId,
        ulong channelId,
        ulong messageId,
        ulong creatorId,
        string question,
        List<string> options,
        TimeSpan? duration)
    {
        await using var ctx = _db.GetDbContext();

        var optionsJson = JsonSerializer.Serialize(options);
        var endsAt = duration.HasValue ? (DateTime?)DateTime.UtcNow + duration.Value : null;

        var poll = await ctx.GetTable<PollModel>()
            .InsertWithOutputAsync(() => new PollModel
            {
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                CreatorId = creatorId,
                Question = question,
                OptionsJson = optionsJson,
                EndsAt = endsAt,
                IsActive = true,
            });

        return poll;
    }

    public async Task<bool> VoteAsync(int pollId, ulong userId, int optionIndex)
    {
        await using var ctx = _db.GetDbContext();

        // Remove previous vote if any
        await ctx.GetTable<PollVote>()
            .DeleteAsync(x => x.PollId == pollId && x.UserId == userId);

        await ctx.GetTable<PollVote>()
            .InsertAsync(() => new PollVote
            {
                PollId = pollId,
                UserId = userId,
                OptionIndex = optionIndex,
            });

        return true;
    }

    public async Task<Dictionary<int, int>> GetVoteCountsAsync(int pollId)
    {
        await using var ctx = _db.GetDbContext();
        var votes = await ctx.GetTable<PollVote>()
            .Where(x => x.PollId == pollId)
            .GroupBy(x => x.OptionIndex)
            .Select(g => new { OptionIndex = g.Key, Count = g.Count() })
            .ToListAsyncLinqToDB();

        return votes.ToDictionary(x => x.OptionIndex, x => x.Count);
    }

    public async Task<bool> EndPollAsync(ulong guildId, int pollId)
    {
        await using var ctx = _db.GetDbContext();
        var poll = await ctx.GetTable<PollModel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == pollId && x.GuildId == guildId && x.IsActive);

        if (poll is null)
            return false;

        await EndPollInternalAsync(poll);
        return true;
    }

    private async Task EndPollInternalAsync(PollModel poll)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<PollModel>()
            .Where(x => x.Id == poll.Id)
            .Set(x => x.IsActive, false)
            .UpdateAsync();

        var voteCounts = await GetVoteCountsAsync(poll.Id);
        var options = JsonSerializer.Deserialize<List<string>>(poll.OptionsJson) ?? new();
        var totalVotes = voteCounts.Values.Sum();

        var guild = _client.GetGuild(poll.GuildId);
        var ch = guild?.GetTextChannel(poll.ChannelId);
        if (ch is null)
            return;

        var results = options.Select((opt, i) =>
        {
            var count = voteCounts.GetValueOrDefault(i, 0);
            var pct = totalVotes > 0 ? (count * 100.0 / totalVotes) : 0;
            var bar = new string('█', (int)(pct / 10)) + new string('░', 10 - (int)(pct / 10));
            return $"**{opt}**\n{bar} {count} ({pct:F1}%)";
        });

        var eb = _sender.CreateEmbed(poll.GuildId)
            .WithTitle($"📊 Poll Ended: {poll.Question}")
            .WithDescription(string.Join("\n\n", results))
            .WithFooter($"Total votes: {totalVotes}")
            .WithOkColor();

        try
        {
            var msg = await ch.GetMessageAsync(poll.MessageId) as IUserMessage;
            if (msg is not null)
                await msg.ModifyAsync(x => { x.Embed = eb.Build(); x.Components = new ComponentBuilder().Build(); });
            else
                await _sender.Response(ch).Embed(eb).SendAsync();
        }
        catch
        {
            await _sender.Response(ch).Embed(eb).SendAsync();
        }
    }

    public async Task<PollModel?> GetPollByMessageAsync(ulong messageId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PollModel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.MessageId == messageId && x.IsActive);
    }

    // Suggestion methods
    public async Task<SuggestionModel?> CreateSuggestionAsync(
        ulong guildId,
        ulong channelId,
        ulong messageId,
        ulong authorId,
        string content)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SuggestionModel>()
            .InsertWithOutputAsync(() => new SuggestionModel
            {
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                AuthorId = authorId,
                Content = content,
                Status = SuggestionStatus.Pending,
            });
    }

    public async Task<bool> UpdateSuggestionStatusAsync(ulong guildId, int id, SuggestionStatus status, string? reason)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<SuggestionModel>()
            .Where(x => x.Id == id && x.GuildId == guildId)
            .Set(x => x.Status, status)
            .Set(x => x.StatusReason, reason ?? "")
            .UpdateAsync();

        return updated > 0;
    }
}
