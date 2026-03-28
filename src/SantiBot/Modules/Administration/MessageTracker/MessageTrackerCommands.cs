using Discord;
using SantiBot.Common.Attributes;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("snipe")]
    [Name("MessageTracker")]
    public partial class MessageTrackerCommands : SantiModule<MessageTrackerService>
    {
        /// <summary>
        /// Show the most recently deleted messages in this channel.
        /// Moderators can see who deleted what and when.
        /// </summary>
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModSnipe(int count = 1)
        {
            if (count < 1) count = 1;
            if (count > 10) count = 10;

            var deleted = _service.GetDeletedMessages(ctx.Channel.Id, count);

            if (deleted.Count == 0)
            {
                await Response().Error(strs.snipe_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Deleted Messages ({deleted.Count})");

            foreach (var msg in deleted)
            {
                var content = string.IsNullOrEmpty(msg.Original.Content)
                    ? "*[no text content]*"
                    : msg.Original.Content.TrimTo(200);

                var attachments = msg.Original.Attachments.Count > 0
                    ? $"\n📎 {msg.Original.Attachments.Count} attachment(s)"
                    : "";

                var timeDiff = DateTime.UtcNow - msg.DeletedAt;
                var timeAgo = timeDiff.TotalMinutes < 1 ? "just now"
                    : timeDiff.TotalMinutes < 60 ? $"{(int)timeDiff.TotalMinutes}m ago"
                    : timeDiff.TotalHours < 24 ? $"{(int)timeDiff.TotalHours}h ago"
                    : $"{(int)timeDiff.TotalDays}d ago";

                eb.AddField(
                    $"{msg.Original.AuthorName} — deleted {timeAgo}",
                    content + attachments,
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        /// <summary>
        /// Show deleted messages by a specific user in this channel.
        /// </summary>
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModSnipe(IGuildUser user, int count = 5)
        {
            if (count < 1) count = 1;
            if (count > 10) count = 10;

            var deleted = _service.GetDeletedByUser(ctx.Channel.Id, user.Id, count);

            if (deleted.Count == 0)
            {
                await Response().Error(strs.snipe_user_none(user.ToString()!)).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Deleted Messages by {user.Username}");

            foreach (var msg in deleted)
            {
                var content = string.IsNullOrEmpty(msg.Original.Content)
                    ? "*[no text content]*"
                    : msg.Original.Content.TrimTo(200);

                var timeDiff = DateTime.UtcNow - msg.DeletedAt;
                var timeAgo = timeDiff.TotalMinutes < 1 ? "just now"
                    : timeDiff.TotalMinutes < 60 ? $"{(int)timeDiff.TotalMinutes}m ago"
                    : $"{(int)timeDiff.TotalHours}h ago";

                eb.AddField($"Deleted {timeAgo}", content, false);
            }

            await Response().Embed(eb).SendAsync();
        }

        /// <summary>
        /// Show the most recently edited messages in this channel.
        /// Shows the before and after content side by side.
        /// </summary>
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task EditSnipe(int count = 1)
        {
            if (count < 1) count = 1;
            if (count > 10) count = 10;

            var edited = _service.GetEditedMessages(ctx.Channel.Id, count);

            if (edited.Count == 0)
            {
                await Response().Error(strs.editsnipe_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Edited Messages ({edited.Count})");

            foreach (var msg in edited)
            {
                var timeDiff = DateTime.UtcNow - msg.EditedAt;
                var timeAgo = timeDiff.TotalMinutes < 1 ? "just now"
                    : timeDiff.TotalMinutes < 60 ? $"{(int)timeDiff.TotalMinutes}m ago"
                    : timeDiff.TotalHours < 24 ? $"{(int)timeDiff.TotalHours}h ago"
                    : $"{(int)timeDiff.TotalDays}d ago";

                eb.AddField(
                    $"{msg.AuthorName} — edited {timeAgo}",
                    $"**Before:** {msg.OldContent.TrimTo(100)}\n**After:** {msg.NewContent.TrimTo(100)}",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
