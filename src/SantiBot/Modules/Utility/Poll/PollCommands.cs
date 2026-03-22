using System.Text.Json;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("poll")]
    [Name("Polls")]
    public partial class PollCommands : SantiModule<PollService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task PollCreate(string question, [Leftover] string optionsStr)
        {
            var options = optionsStr.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (options.Count < 2 || options.Count > 10)
            {
                await Response().Error(strs.poll_options_invalid).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"📊 {question}")
                .WithDescription(string.Join("\n",
                    options.Select((o, i) => $"{GetNumberEmoji(i)} **{o}**")))
                .WithFooter("Click a button to vote!")
                .WithOkColor();

            var cb = new ComponentBuilder();
            for (var i = 0; i < options.Count; i++)
            {
                cb.WithButton(options[i].TrimTo(80), $"poll:vote:{i}",
                    ButtonStyle.Primary, emote: new Emoji(GetNumberEmoji(i)));
            }

            var msg = await ctx.Channel.SendMessageAsync(embed: eb.Build(), components: cb.Build());

            await _service.CreatePollAsync(
                ctx.Guild.Id,
                ctx.Channel.Id,
                msg.Id,
                ctx.User.Id,
                question,
                options,
                null);

            _ = ctx.Message.DeleteAfter(3);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task PollCreateTimed(TimeSpan duration, string question, [Leftover] string optionsStr)
        {
            if (duration > TimeSpan.FromDays(30) || duration < TimeSpan.FromMinutes(1))
            {
                await Response().Error(strs.poll_duration_invalid).SendAsync();
                return;
            }

            var options = optionsStr.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (options.Count < 2 || options.Count > 10)
            {
                await Response().Error(strs.poll_options_invalid).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"📊 {question}")
                .WithDescription(string.Join("\n",
                    options.Select((o, i) => $"{GetNumberEmoji(i)} **{o}**")))
                .AddField("Ends", TimestampTag.FromDateTime(DateTime.UtcNow + duration, TimestampTagStyles.Relative))
                .WithFooter("Click a button to vote!")
                .WithOkColor();

            var cb = new ComponentBuilder();
            for (var i = 0; i < options.Count; i++)
            {
                cb.WithButton(options[i].TrimTo(80), $"poll:vote:{i}",
                    ButtonStyle.Primary, emote: new Emoji(GetNumberEmoji(i)));
            }

            var msg = await ctx.Channel.SendMessageAsync(embed: eb.Build(), components: cb.Build());

            await _service.CreatePollAsync(
                ctx.Guild.Id,
                ctx.Channel.Id,
                msg.Id,
                ctx.User.Id,
                question,
                options,
                duration);

            _ = ctx.Message.DeleteAfter(3);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task PollEnd(int id)
        {
            if (await _service.EndPollAsync(ctx.Guild.Id, id))
                await Response().Confirm(strs.poll_ended).SendAsync();
            else
                await Response().Error(strs.poll_not_found).SendAsync();
        }

        private static string GetNumberEmoji(int i)
            => i switch
            {
                0 => "1️⃣",
                1 => "2️⃣",
                2 => "3️⃣",
                3 => "4️⃣",
                4 => "5️⃣",
                5 => "6️⃣",
                6 => "7️⃣",
                7 => "8️⃣",
                8 => "9️⃣",
                9 => "🔟",
                _ => "▪️"
            };
    }

    [Group("suggest")]
    [Name("Suggestions")]
    public partial class SuggestionCommands : SantiModule<PollService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Suggest([Leftover] string content)
        {
            var eb = CreateEmbed()
                .WithAuthor(ctx.User)
                .WithTitle("💡 Suggestion")
                .WithDescription(content)
                .AddField("Status", "⏳ Pending")
                .WithOkColor();

            var msg = await Response().Embed(eb).SendAsync();

            // Add upvote/downvote reactions
            await msg.AddReactionAsync(new Emoji("👍"));
            await msg.AddReactionAsync(new Emoji("👎"));

            await _service.CreateSuggestionAsync(
                ctx.Guild.Id,
                ctx.Channel.Id,
                msg.Id,
                ctx.User.Id,
                content);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task SuggestApprove(int id, [Leftover] string? reason = null)
        {
            if (await _service.UpdateSuggestionStatusAsync(ctx.Guild.Id, id, SuggestionStatus.Approved, reason))
                await Response().Confirm(strs.suggestion_approved).SendAsync();
            else
                await Response().Error(strs.suggestion_not_found).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task SuggestDeny(int id, [Leftover] string? reason = null)
        {
            if (await _service.UpdateSuggestionStatusAsync(ctx.Guild.Id, id, SuggestionStatus.Denied, reason))
                await Response().Confirm(strs.suggestion_denied).SendAsync();
            else
                await Response().Error(strs.suggestion_not_found).SendAsync();
        }
    }
}
