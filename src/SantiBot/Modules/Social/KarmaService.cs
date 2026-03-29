#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class KarmaService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public KarmaService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.ReactionAdded += OnReactionAdded;
        _client.ReactionRemoved += OnReactionRemoved;
        return Task.CompletedTask;
    }

    private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> ch, SocketReaction reaction)
    {
        try
        {
            if (reaction.UserId == msg.Id) return; // can't karma self
            var channel = await ch.GetOrDownloadAsync();
            if (channel is not ITextChannel tc) return;

            var emote = reaction.Emote.Name;
            bool? isUp = emote switch
            {
                "\u2B06\uFE0F" => true,  // up arrow
                "\u2B07\uFE0F" => false,  // down arrow
                _ => null
            };
            if (isUp is null) return;

            var message = await msg.GetOrDownloadAsync();
            if (message?.Author is null || message.Author.IsBot) return;

            await ProcessVoteAsync(tc.GuildId, reaction.UserId, message.Author.Id, msg.Id, isUp.Value);
        }
        catch { /* ignore */ }
    }

    private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> ch, SocketReaction reaction)
    {
        try
        {
            var channel = await ch.GetOrDownloadAsync();
            if (channel is not ITextChannel tc) return;

            var emote = reaction.Emote.Name;
            if (emote != "\u2B06\uFE0F" && emote != "\u2B07\uFE0F") return;

            await RemoveVoteAsync(tc.GuildId, reaction.UserId, msg.Id);
        }
        catch { /* ignore */ }
    }

    private async Task ProcessVoteAsync(ulong guildId, ulong voterId, ulong targetId, ulong messageId, bool isUpvote)
    {
        if (voterId == targetId) return;

        await using var ctx = _db.GetDbContext();

        // remove existing vote on same message
        var existing = await ctx.GetTable<KarmaVote>()
            .Where(x => x.GuildId == guildId && x.VoterId == voterId && x.MessageId == messageId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is not null)
        {
            // undo previous vote
            if (existing.IsUpvote)
                await ctx.GetTable<UserKarma>()
                    .Where(x => x.GuildId == guildId && x.UserId == existing.TargetUserId)
                    .Set(x => x.Upvotes, x => x.Upvotes - 1)
                    .UpdateAsync();
            else
                await ctx.GetTable<UserKarma>()
                    .Where(x => x.GuildId == guildId && x.UserId == existing.TargetUserId)
                    .Set(x => x.Downvotes, x => x.Downvotes - 1)
                    .UpdateAsync();

            await ctx.GetTable<KarmaVote>()
                .Where(x => x.Id == existing.Id)
                .DeleteAsync();
        }

        // ensure karma row exists
        var karma = await ctx.GetTable<UserKarma>()
            .Where(x => x.GuildId == guildId && x.UserId == targetId)
            .FirstOrDefaultAsyncLinqToDB();

        if (karma is null)
        {
            await ctx.GetTable<UserKarma>()
                .InsertAsync(() => new UserKarma
                {
                    GuildId = guildId,
                    UserId = targetId,
                    Upvotes = isUpvote ? 1 : 0,
                    Downvotes = isUpvote ? 0 : 1
                });
        }
        else
        {
            if (isUpvote)
                await ctx.GetTable<UserKarma>()
                    .Where(x => x.GuildId == guildId && x.UserId == targetId)
                    .Set(x => x.Upvotes, x => x.Upvotes + 1)
                    .UpdateAsync();
            else
                await ctx.GetTable<UserKarma>()
                    .Where(x => x.GuildId == guildId && x.UserId == targetId)
                    .Set(x => x.Downvotes, x => x.Downvotes + 1)
                    .UpdateAsync();
        }

        // record vote
        await ctx.GetTable<KarmaVote>()
            .InsertAsync(() => new KarmaVote
            {
                GuildId = guildId,
                VoterId = voterId,
                TargetUserId = targetId,
                MessageId = messageId,
                IsUpvote = isUpvote
            });
    }

    private async Task RemoveVoteAsync(ulong guildId, ulong voterId, ulong messageId)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<KarmaVote>()
            .Where(x => x.GuildId == guildId && x.VoterId == voterId && x.MessageId == messageId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null) return;

        if (existing.IsUpvote)
            await ctx.GetTable<UserKarma>()
                .Where(x => x.GuildId == guildId && x.UserId == existing.TargetUserId)
                .Set(x => x.Upvotes, x => x.Upvotes - 1)
                .UpdateAsync();
        else
            await ctx.GetTable<UserKarma>()
                .Where(x => x.GuildId == guildId && x.UserId == existing.TargetUserId)
                .Set(x => x.Downvotes, x => x.Downvotes - 1)
                .UpdateAsync();

        await ctx.GetTable<KarmaVote>()
            .Where(x => x.Id == existing.Id)
            .DeleteAsync();
    }

    public async Task<UserKarma> GetKarmaAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserKarma>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB()
            ?? new UserKarma { GuildId = guildId, UserId = userId, Upvotes = 0, Downvotes = 0 };
    }

    public async Task<List<UserKarma>> GetKarmaLeaderboardAsync(ulong guildId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserKarma>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Upvotes - x.Downvotes)
            .Take(count)
            .ToListAsyncLinqToDB();
    }
}
