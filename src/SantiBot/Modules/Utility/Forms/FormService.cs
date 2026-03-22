using System.Text.Json;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class FormService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;

    public FormService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public async Task<FormModel?> CreateFormAsync(
        ulong guildId,
        ulong creatorId,
        string title,
        List<string> questions,
        ulong responseChannelId)
    {
        await using var ctx = _db.GetDbContext();

        var questionsJson = JsonSerializer.Serialize(questions);

        return await ctx.GetTable<FormModel>()
            .InsertWithOutputAsync(() => new FormModel
            {
                GuildId = guildId,
                CreatorId = creatorId,
                Title = title,
                QuestionsJson = questionsJson,
                ResponseChannelId = responseChannelId,
                IsActive = true,
            });
    }

    public async Task<List<FormModel>> GetFormsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<FormModel>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .ToListAsyncLinqToDB();
    }

    public async Task<FormModel?> GetFormAsync(ulong guildId, int formId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<FormModel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == formId && x.GuildId == guildId && x.IsActive);
    }

    public async Task<bool> DeleteFormAsync(ulong guildId, int formId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<FormModel>()
            .Where(x => x.Id == formId && x.GuildId == guildId)
            .Set(x => x.IsActive, false)
            .UpdateAsync();
        return deleted > 0;
    }

    public async Task<bool> SubmitResponseAsync(int formId, ulong userId, List<string> answers)
    {
        await using var ctx = _db.GetDbContext();

        var form = await ctx.GetTable<FormModel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == formId && x.IsActive);

        if (form is null)
            return false;

        var answersJson = JsonSerializer.Serialize(answers);
        var now = DateTime.UtcNow;

        await ctx.GetTable<FormResponse>()
            .InsertAsync(() => new FormResponse
            {
                FormId = formId,
                UserId = userId,
                AnswersJson = answersJson,
                SubmittedAt = now,
            });

        // Post response to the designated channel
        var guild = _client.GetGuild(form.GuildId);
        var responseCh = guild?.GetTextChannel(form.ResponseChannelId);
        if (responseCh is null)
            return true;

        var questions = JsonSerializer.Deserialize<List<string>>(form.QuestionsJson) ?? new();

        var eb = _sender.CreateEmbed(form.GuildId)
            .WithTitle($"📋 {form.Title} — New Response")
            .WithFooter($"User ID: {userId} | Form ID: {formId}")
            .WithOkColor();

        var user = guild?.GetUser(userId);
        if (user is not null)
            eb.WithAuthor(user);

        for (var i = 0; i < questions.Count && i < answers.Count; i++)
        {
            eb.AddField(questions[i], answers[i].TrimTo(1024));
        }

        await _sender.Response(responseCh).Embed(eb).SendAsync();
        return true;
    }

    public async Task<bool> RunFormDmAsync(IDMChannel dm, FormModel form, ulong userId)
    {
        var questions = JsonSerializer.Deserialize<List<string>>(form.QuestionsJson) ?? new();
        if (questions.Count == 0)
            return false;

        var answers = new List<string>();

        await _sender.Response(dm)
            .Embed(_sender.CreateEmbed()
                .WithTitle($"📋 {form.Title}")
                .WithDescription($"Please answer the following {questions.Count} question(s).\nType `cancel` at any time to stop.")
                .WithOkColor())
            .SendAsync();

        foreach (var question in questions)
        {
            await _sender.Response(dm)
                .Text($"**{question}**")
                .SendAsync();

            var response = await WaitForDmResponseAsync(dm, userId, TimeSpan.FromMinutes(10));
            if (response is null)
            {
                await _sender.Response(dm).Text("⏰ Timed out. Form submission cancelled.").SendAsync();
                return false;
            }

            if (response.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                await _sender.Response(dm).Text("❌ Form submission cancelled.").SendAsync();
                return false;
            }

            answers.Add(response);
        }

        await SubmitResponseAsync(form.Id, userId, answers);
        await _sender.Response(dm).Text("✅ Your response has been submitted. Thank you!").SendAsync();
        return true;
    }

    private async Task<string?> WaitForDmResponseAsync(IDMChannel dm, ulong userId, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        Task Handler(SocketMessage msg)
        {
            if (msg.Author.Id == userId && msg.Channel.Id == dm.Id)
                tcs.TrySetResult(msg.Content);
            return Task.CompletedTask;
        }

        _client.MessageReceived += Handler;
        try
        {
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            return completed == tcs.Task ? tcs.Task.Result : null;
        }
        finally
        {
            _client.MessageReceived -= Handler;
        }
    }
}
