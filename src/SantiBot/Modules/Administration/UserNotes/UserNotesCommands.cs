#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("usernotes")]
    public partial class UserNotesCommands : SantiModule<UserNotesService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task UserNotes(IGuildUser user)
        {
            var timeline = await _service.GetTimelineAsync(ctx.Guild.Id, user.Id);
            if (timeline.Count == 0)
            {
                await Response().Error($"No notes or actions recorded for {user.Mention}.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle($"Timeline: {user}")
                .WithOkColor();

            var actionEmojis = new Dictionary<string, string>
            {
                ["note"] = "\ud83d\udcdd",
                ["warn"] = "\u26a0\ufe0f",
                ["mute"] = "\ud83d\udd07",
                ["ban"] = "\ud83d\udd28",
                ["kick"] = "\ud83d\udc62"
            };

            foreach (var n in timeline.Take(20))
            {
                var emoji = actionEmojis.GetValueOrDefault(n.ActionType, "\ud83d\udcdd");
                embed.AddField($"{emoji} [{n.ActionType.ToUpperInvariant()}] #{n.Id} - {n.DateAdded:g}",
                    $"By <@{n.ModeratorId}>: {n.Note}");
            }

            if (timeline.Count > 20)
                embed.WithFooter($"...and {timeline.Count - 20} more entries");

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AddNote(IGuildUser user, [Leftover] string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                await Response().Error("Note cannot be empty.").SendAsync();
                return;
            }

            var result = await _service.AddNoteAsync(ctx.Guild.Id, user.Id, ctx.User.Id, note);
            await Response().Confirm($"Note **#{result.Id}** added for {user.Mention}.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task DelNote(int id)
        {
            if (await _service.DeleteNoteAsync(ctx.Guild.Id, id))
                await Response().Confirm($"Note **#{id}** deleted.").SendAsync();
            else
                await Response().Error("Note not found.").SendAsync();
        }
    }
}
