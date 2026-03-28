using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Newtonsoft.Json;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class WelcomeQuizService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    public WelcomeQuizService(DbService db, DiscordSocketClient client, IBotCreds creds)
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
            var config = await GetConfigAsync(user.Guild.Id);
            if (config is null || !config.Enabled || config.VerifiedRoleId is null)
                return;

            var questions = DeserializeQuestions(config.QuestionsJson);
            if (questions.Count == 0)
                return;

            IMessageChannel channel;
            if (config.QuizChannelId.HasValue)
            {
                channel = user.Guild.GetTextChannel(config.QuizChannelId.Value);
                if (channel is null)
                    return;
            }
            else
            {
                channel = await user.CreateDMChannelAsync();
            }

            var correctCount = 0;
            foreach (var q in questions)
            {
                var answersText = string.Join("\n", q.Answers.Select((a, i) => $"**{i + 1}.** {a}"));
                var msg = $"**{q.Question}**\n{answersText}\n\nReply with the number of the correct answer.";

                await channel.SendMessageAsync($"{user.Mention}\n{msg}");

                // Wait for response (30 seconds per question)
                var response = await WaitForResponseAsync(channel, user.Id, TimeSpan.FromSeconds(30));
                if (response is not null
                    && int.TryParse(response.Content.Trim(), out var answerNum)
                    && answerNum - 1 == q.CorrectIndex)
                {
                    correctCount++;
                }
            }

            if (correctCount >= config.RequiredCorrect)
            {
                var role = user.Guild.GetRole(config.VerifiedRoleId.Value);
                if (role is not null)
                {
                    await user.AddRoleAsync(role);
                    await channel.SendMessageAsync(
                        $"{user.Mention} You passed the welcome quiz! You have been verified.");
                }
            }
            else
            {
                await channel.SendMessageAsync(
                    $"{user.Mention} You got {correctCount}/{config.RequiredCorrect} correct. " +
                    "You did not pass the quiz. Please contact a moderator for help.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in welcome quiz for user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
    }

    private async Task<IMessage?> WaitForResponseAsync(
        IMessageChannel channel, ulong userId, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<IMessage?>();

        Task Handler(SocketMessage msg)
        {
            if (msg.Author.Id == userId && msg.Channel.Id == channel.Id)
                tcs.TrySetResult(msg);
            return Task.CompletedTask;
        }

        _client.MessageReceived += Handler;
        try
        {
            var result = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            return result == tcs.Task ? tcs.Task.Result : null;
        }
        finally
        {
            _client.MessageReceived -= Handler;
        }
    }

    public async Task<WelcomeQuizConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<WelcomeQuizConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<WelcomeQuizConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<WelcomeQuizConfig>()
                .InsertAsync(() => new WelcomeQuizConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                });
        }
        else
        {
            await ctx.GetTable<WelcomeQuizConfig>()
                .Where(x => x.GuildId == guildId)
                .UpdateAsync(x => new WelcomeQuizConfig { Enabled = enabled });
        }
    }

    public async Task SetRoleAsync(ulong guildId, ulong roleId)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<WelcomeQuizConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new WelcomeQuizConfig { VerifiedRoleId = roleId });
    }

    public async Task SetChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<WelcomeQuizConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new WelcomeQuizConfig { QuizChannelId = channelId });
    }

    public async Task SetRequiredCorrectAsync(ulong guildId, int required)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<WelcomeQuizConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new WelcomeQuizConfig { RequiredCorrect = required });
    }

    public async Task AddQuestionAsync(ulong guildId, string question, string[] answers, int correctIndex)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);

        var config = await ctx.GetTable<WelcomeQuizConfig>()
            .FirstAsync(x => x.GuildId == guildId);

        var questions = DeserializeQuestions(config.QuestionsJson);
        questions.Add(new QuizQuestion { Question = question, Answers = answers.ToList(), CorrectIndex = correctIndex });

        var json = JsonConvert.SerializeObject(questions);
        await ctx.GetTable<WelcomeQuizConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new WelcomeQuizConfig { QuestionsJson = json });
    }

    public async Task<bool> RemoveQuestionAsync(ulong guildId, int index)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<WelcomeQuizConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is null)
            return false;

        var questions = DeserializeQuestions(config.QuestionsJson);
        if (index < 0 || index >= questions.Count)
            return false;

        questions.RemoveAt(index);
        var json = JsonConvert.SerializeObject(questions);

        await ctx.GetTable<WelcomeQuizConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new WelcomeQuizConfig { QuestionsJson = json });

        return true;
    }

    public List<QuizQuestion> GetQuestions(string json)
        => DeserializeQuestions(json);

    private static List<QuizQuestion> DeserializeQuestions(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<List<QuizQuestion>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static async Task EnsureConfigAsync(SantiContext ctx, ulong guildId)
    {
        var exists = await ctx.GetTable<WelcomeQuizConfig>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId);

        if (!exists)
        {
            await ctx.GetTable<WelcomeQuizConfig>()
                .InsertAsync(() => new WelcomeQuizConfig { GuildId = guildId });
        }
    }
}

public class QuizQuestion
{
    public string Question { get; set; } = "";
    public List<string> Answers { get; set; } = new();
    public int CorrectIndex { get; set; }
}
