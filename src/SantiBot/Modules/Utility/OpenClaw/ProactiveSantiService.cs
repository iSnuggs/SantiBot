#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.OpenClaw;

/// <summary>
/// Handles scheduled AI posts — Santi can proactively post daily facts,
/// server summaries, motivational messages, etc. in configured channels.
/// </summary>
public sealed class ProactiveSantiService : INService
{
    private readonly DbService _db;
    private readonly OpenClawService _oc;
    private readonly DiscordSocketClient _client;
    private static readonly SantiRandom _rng = new();

    // Built-in post types and their prompts
    public static readonly Dictionary<string, (string Name, string Emoji, string Prompt)> PostTypes = new()
    {
        ["DailyFact"] = ("Daily Fun Fact", "🧠",
            "Share one interesting, surprising fun fact that most people don't know. Keep it to 2-3 sentences. Be enthusiastic!"),
        ["ServerSummary"] = ("Server Summary", "📊",
            "Give a brief, friendly 3-4 line summary of what's happening. Mention it's a great day to be part of the community. Add a fun emoji."),
        ["Motivation"] = ("Daily Motivation", "💪",
            "Share an inspiring quote or motivational message. Keep it genuine and uplifting, 2-3 sentences. Include who said it if it's a quote."),
        ["DadJoke"] = ("Daily Dad Joke", "😄",
            "Tell a clean, family-friendly dad joke. Setup and punchline format."),
        ["GameTip"] = ("SantiBot Game Tip", "🎮",
            "Share a helpful tip about SantiBot's features — like a dungeon class tip, a crafting recipe hint, a pet care tip, or a raid boss strategy. Be specific and helpful. 2-3 sentences."),
        ["Riddle"] = ("Daily Riddle", "🤔",
            "Share a fun riddle or brain teaser. Give the riddle first, then put the answer in a Discord spoiler tag ||like this||."),
        ["ThisDayInHistory"] = ("This Day in History", "📅",
            "Share one interesting thing that happened on today's date in history. Keep it to 2-3 sentences. Include the year."),
        ["WouldYouRather"] = ("Would You Rather", "🤷",
            "Ask a fun, creative 'Would you rather' question. Make it thought-provoking but lighthearted. Just the question, no answer."),
    };

    public ProactiveSantiService(DbService db, OpenClawService oc, DiscordSocketClient client)
    {
        _db = db;
        _oc = oc;
        _client = client;
    }

    public async Task<SantiScheduledPost> AddScheduledPostAsync(ulong guildId, ulong channelId, string postType, string cronSchedule, ulong createdBy, string customPrompt = null)
    {
        await using var ctx = _db.GetDbContext();
        var post = new SantiScheduledPost
        {
            GuildId = guildId,
            ChannelId = channelId,
            PostType = postType,
            CronSchedule = cronSchedule,
            CreatedBy = createdBy,
            CustomPrompt = customPrompt,
        };
        ctx.Add(post);
        await ctx.SaveChangesAsync();
        return post;
    }

    public async Task<List<SantiScheduledPost>> GetScheduledPostsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SantiScheduledPost>()
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.PostType)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> RemoveScheduledPostAsync(ulong guildId, int postId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SantiScheduledPost>()
            .Where(x => x.GuildId == guildId && x.Id == postId)
            .DeleteAsync() > 0;
    }

    public async Task<bool> TogglePostAsync(ulong guildId, int postId)
    {
        await using var ctx = _db.GetDbContext();
        var post = await ctx.GetTable<SantiScheduledPost>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Id == postId);
        if (post is null) return false;

        await ctx.GetTable<SantiScheduledPost>()
            .Where(x => x.Id == postId)
            .UpdateAsync(_ => new SantiScheduledPost { IsEnabled = !post.IsEnabled });
        return true;
    }

    /// <summary>Execute a scheduled post — called by a background timer or cron</summary>
    public async Task<(bool Success, string Message)> ExecutePostAsync(SantiScheduledPost post)
    {
        var prompt = post.PostType == "Custom" && !string.IsNullOrWhiteSpace(post.CustomPrompt)
            ? post.CustomPrompt
            : PostTypes.TryGetValue(post.PostType, out var pt) ? pt.Prompt : null;

        if (prompt is null)
            return (false, "Unknown post type");

        var (success, response) = await _oc.QuickAskAsync(prompt);
        if (!success)
            return (false, "OpenClaw didn't respond");

        // Post to the channel
        try
        {
            var guild = _client.GetGuild(post.GuildId);
            var channel = guild?.GetTextChannel(post.ChannelId);
            if (channel is null)
                return (false, "Channel not found");

            var typeDef = PostTypes.TryGetValue(post.PostType, out var td) ? td : ("Santi", "🐾", "");

            var embed = new EmbedBuilder()
                .WithAuthor($"{typeDef.Item2} {typeDef.Item1}", _client.CurrentUser.GetAvatarUrl())
                .WithDescription(response)
                .WithColor(new Color(0x00E68A))
                .WithFooter("Powered by Santi AI 🐾")
                .WithCurrentTimestamp()
                .Build();

            await channel.SendMessageAsync(embed: embed);

            // Update last posted time
            await using var ctx = _db.GetDbContext();
            await ctx.GetTable<SantiScheduledPost>()
                .Where(x => x.Id == post.Id)
                .UpdateAsync(_ => new SantiScheduledPost { LastPostedAt = DateTime.UtcNow });

            return (true, response);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>Manually trigger a post type in a channel (for testing)</summary>
    public async Task<(bool Success, string Response)> PostNowAsync(ulong guildId, ulong channelId, string postType)
    {
        var tempPost = new SantiScheduledPost
        {
            GuildId = guildId,
            ChannelId = channelId,
            PostType = postType,
        };
        return await ExecutePostAsync(tempPost);
    }
}
