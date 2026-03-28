namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("welcomequiz")]
    [Name("WelcomeQuiz")]
    public partial class WelcomeQuizCommands : SantiModule<WelcomeQuizService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeQuizEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id, true);
            await Response().Confirm(strs.welcomequiz_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeQuizDisable()
        {
            await _service.EnableAsync(ctx.Guild.Id, false);
            await Response().Confirm(strs.welcomequiz_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeQuizRole(IRole role)
        {
            await _service.SetRoleAsync(ctx.Guild.Id, role.Id);
            await Response().Confirm(strs.welcomequiz_role_set(role.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeQuizChannel(ITextChannel channel)
        {
            await _service.SetChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.welcomequiz_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeQuizAdd(string question, [Leftover] string rest)
        {
            // Parse: "answer1" "answer2" "answer3" correctIndex
            var parts = ParseQuotedArgs(rest);
            if (parts.Count < 3) // at least 2 answers + correctIndex
            {
                await Response().Error(strs.welcomequiz_add_usage).SendAsync();
                return;
            }

            if (!int.TryParse(parts[^1], out var correctIndex))
            {
                await Response().Error(strs.welcomequiz_add_usage).SendAsync();
                return;
            }

            var answers = parts.Take(parts.Count - 1).ToArray();
            if (correctIndex < 0 || correctIndex >= answers.Length)
            {
                await Response().Error(strs.welcomequiz_invalid_index).SendAsync();
                return;
            }

            await _service.AddQuestionAsync(ctx.Guild.Id, question, answers, correctIndex);
            await Response().Confirm(strs.welcomequiz_question_added(question)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeQuizRemove(int index)
        {
            // User-facing is 1-based
            if (await _service.RemoveQuestionAsync(ctx.Guild.Id, index - 1))
                await Response().Confirm(strs.welcomequiz_question_removed(index)).SendAsync();
            else
                await Response().Error(strs.welcomequiz_question_not_found).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeQuizList()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            if (config is null)
            {
                await Response().Error(strs.welcomequiz_not_configured).SendAsync();
                return;
            }

            var questions = _service.GetQuestions(config.QuestionsJson);
            if (questions.Count == 0)
            {
                await Response().Error(strs.welcomequiz_no_questions).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.welcomequiz_list_title))
                .WithFooter($"Enabled: {config.Enabled} | Required correct: {config.RequiredCorrect}");

            for (var i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                var answersText = string.Join(", ", q.Answers.Select((a, j) =>
                    j == q.CorrectIndex ? $"**{a}** (correct)" : a));
                eb.AddField($"#{i + 1}: {q.Question}", answersText);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeQuizRequired(int count)
        {
            if (count < 1)
            {
                await Response().Error(strs.welcomequiz_required_invalid).SendAsync();
                return;
            }

            await _service.SetRequiredCorrectAsync(ctx.Guild.Id, count);
            await Response().Confirm(strs.welcomequiz_required_set(count)).SendAsync();
        }

        private static List<string> ParseQuotedArgs(string input)
        {
            var results = new List<string>();
            var inQuote = false;
            var current = new System.Text.StringBuilder();

            foreach (var ch in input)
            {
                if (ch == '"')
                {
                    if (inQuote)
                    {
                        results.Add(current.ToString());
                        current.Clear();
                    }
                    inQuote = !inQuote;
                }
                else if (ch == ' ' && !inQuote)
                {
                    if (current.Length > 0)
                    {
                        results.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }

            if (current.Length > 0)
                results.Add(current.ToString());

            return results;
        }
    }
}
