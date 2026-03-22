using System.Text.Json;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("form")]
    [Name("Forms")]
    public partial class FormCommands : SantiModule<FormService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task FormCreate(ITextChannel responseChannel, string title, [Leftover] string questionsStr)
        {
            var questions = questionsStr.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (questions.Count == 0 || questions.Count > 20)
            {
                await Response().Error(strs.form_questions_invalid).SendAsync();
                return;
            }

            var form = await _service.CreateFormAsync(
                ctx.Guild.Id,
                ctx.User.Id,
                title,
                questions,
                responseChannel.Id);

            if (form is null)
            {
                await Response().Error(strs.form_create_failed).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"📋 Form Created: {title}")
                .WithDescription($"**ID:** {form.Id}\n**Questions:** {questions.Count}\n**Responses go to:** {responseChannel.Mention}")
                .AddField("Questions", string.Join("\n", questions.Select((q, i) => $"{i + 1}. {q}")));

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task FormList()
        {
            var forms = await _service.GetFormsAsync(ctx.Guild.Id);

            if (forms.Count == 0)
            {
                await Response().Error(strs.form_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("📋 Active Forms");

            foreach (var form in forms)
            {
                var questions = JsonSerializer.Deserialize<List<string>>(form.QuestionsJson) ?? new();
                eb.AddField($"ID: {form.Id} — {form.Title}", $"{questions.Count} questions | Responses → <#{form.ResponseChannelId}>");
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task FormDelete(int id)
        {
            if (await _service.DeleteFormAsync(ctx.Guild.Id, id))
                await Response().Confirm(strs.form_deleted).SendAsync();
            else
                await Response().Error(strs.form_not_found).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FormFill(int id)
        {
            var form = await _service.GetFormAsync(ctx.Guild.Id, id);
            if (form is null)
            {
                await Response().Error(strs.form_not_found).SendAsync();
                return;
            }

            IDMChannel dm;
            try
            {
                dm = await ctx.User.CreateDMChannelAsync();
            }
            catch
            {
                await Response().Error(strs.form_dm_failed).SendAsync();
                return;
            }

            await Response().Confirm(strs.form_dm_sent).SendAsync();
            _ = Task.Run(() => _service.RunFormDmAsync(dm, form, ctx.User.Id));
        }
    }
}
