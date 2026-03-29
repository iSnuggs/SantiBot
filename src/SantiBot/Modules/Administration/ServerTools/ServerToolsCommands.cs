#nullable disable
using SantiBot.Modules.Administration.ServerTools;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Name("Server Tools")]
    [Group("st")]
    public partial class ServerToolsCommands : SantiModule<ServerToolsService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task InviteRewards(int threshold, IRole role)
        {
            var validThresholds = new[] { 5, 10, 25, 50 };
            if (!validThresholds.Contains(threshold))
            {
                await Response()
                    .Error($"Invalid threshold. Valid thresholds: {string.Join(", ", validThresholds)}")
                    .SendAsync();
                return;
            }

            _service.SetInviteRewardRole(ctx.Guild.Id, threshold, role.Id);
            await Response()
                .Confirm($"Users who invite **{threshold}** people will receive the **{role.Name}** role.")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task InviteRewards()
        {
            var roles = _service.GetInviteRewardRoles(ctx.Guild.Id);
            if (roles.Count == 0)
            {
                await Response().Error("No invite reward roles configured.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Invite Reward Roles");

            foreach (var kvp in roles.OrderBy(x => x.Key))
            {
                var role = ctx.Guild.GetRole(kvp.Value);
                eb.AddField($"{kvp.Key} Invites", role?.Mention ?? $"Unknown Role ({kvp.Value})", true);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task InviteCount(IUser user = null)
        {
            user ??= ctx.User;
            var count = _service.GetInviteCount(ctx.Guild.Id, user.Id);

            await Response()
                .Confirm($"**{user.Username}** has invited **{count}** people to this server.")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task RecordInvite(IUser inviter)
        {
            await _service.RecordInviteAsync(ctx.Guild.Id, inviter.Id);
            var count = _service.GetInviteCount(ctx.Guild.Id, inviter.Id);

            await Response()
                .Confirm($"Recorded invite for **{inviter.Username}**. They now have **{count}** invites.")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EmojiStats()
        {
            var topEmojis = _service.GetTopEmojis(ctx.Guild.Id, 15);
            if (topEmojis.Count == 0)
            {
                await Response().Error("No emoji usage data collected yet.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Top Emoji Usage");

            var desc = string.Join('\n',
                topEmojis.Select((kvp, i) => $"**{i + 1}.** {kvp.Key} — used **{kvp.Value}** times"));

            eb.WithDescription(desc);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task EmojiStatsReset()
        {
            _service.ResetEmojiStats(ctx.Guild.Id);
            await Response().Confirm("Emoji statistics have been reset.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task AutoPublish(ITextChannel channel)
        {
            var enabled = _service.ToggleAutoPublish(ctx.Guild.Id, channel.Id);
            await Response()
                .Confirm(enabled
                    ? $"Auto-publish **enabled** for {channel.Mention}. Messages in this announcement channel will be published automatically."
                    : $"Auto-publish **disabled** for {channel.Mention}.")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task AutoPublishList()
        {
            var channels = _service.GetAutoPublishChannels(ctx.Guild.Id);
            if (channels.Count == 0)
            {
                await Response().Error("No auto-publish channels configured.").SendAsync();
                return;
            }

            var list = string.Join('\n', channels.Select(id => $"<#{id}>"));
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Auto-Publish Channels")
                .WithDescription(list);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task Mirror(ITextChannel source, ITextChannel dest)
        {
            if (source.Id == dest.Id)
            {
                await Response().Error("Source and destination channels cannot be the same.").SendAsync();
                return;
            }

            _service.SetMirror(ctx.Guild.Id, source.Id, dest.Id);
            await Response()
                .Confirm($"Messages from {source.Mention} will now be mirrored to {dest.Mention}.")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task MirrorRemove(ITextChannel source)
        {
            if (_service.RemoveMirror(ctx.Guild.Id, source.Id))
                await Response().Confirm($"Mirroring from {source.Mention} has been removed.").SendAsync();
            else
                await Response().Error($"No mirror configured for {source.Mention}.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task MirrorList()
        {
            var mirrors = _service.GetMirrors(ctx.Guild.Id);
            if (mirrors.Count == 0)
            {
                await Response().Error("No message mirrors configured.").SendAsync();
                return;
            }

            var list = string.Join('\n', mirrors.Select(m => $"<#{m.Key}> → <#{m.Value}>"));
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Message Mirrors")
                .WithDescription(list);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task MilestoneSetup(ITextChannel channel)
        {
            _service.SetMilestoneChannel(ctx.Guild.Id, channel.Id);
            await Response()
                .Confirm($"Member milestone announcements will be sent to {channel.Mention}.\n" +
                         $"Milestones: 100, 250, 500, 1K, 2.5K, 5K, 10K, 25K, 50K, 100K members.")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task MilestoneDisable()
        {
            _service.DisableMilestones(ctx.Guild.Id);
            await Response().Confirm("Member milestone announcements have been disabled.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task BackupSummary()
        {
            var guild = (SocketGuild)ctx.Guild;
            var summary = _service.GenerateBackupSummary(guild);

            // Send as a text file attachment since summaries can be long
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(summary));
            var fileName = $"backup_{guild.Name.Replace(' ', '_')}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";

            await ctx.Channel.SendFileAsync(stream, fileName, "Here is your server backup summary:");
        }
    }
}
