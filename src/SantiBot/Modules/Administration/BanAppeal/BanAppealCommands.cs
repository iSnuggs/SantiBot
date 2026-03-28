namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("banappeal")]
    [Name("BanAppeal")]
    public partial class BanAppealCommands : SantiModule<BanAppealService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task BanAppeal()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config is null)
            {
                await Response().Error(strs.banappeal_not_configured).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Ban Appeal Configuration")
                .AddField("Status", config.Enabled ? "Enabled" : "Disabled", true)
                .AddField("Review Channel", config.ReviewChannelId.HasValue ? $"<#{config.ReviewChannelId}>" : "Not set", true)
                .AddField("Appeal Message", config.AppealMessage);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task BanAppealEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id);
            await Response().Confirm(strs.banappeal_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task BanAppealDisable()
        {
            if (await _service.DisableAsync(ctx.Guild.Id))
                await Response().Confirm(strs.banappeal_disabled).SendAsync();
            else
                await Response().Error(strs.banappeal_not_configured).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task BanAppealChannel(ITextChannel channel)
        {
            await _service.SetReviewChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.banappeal_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task BanAppealList()
        {
            var pending = await _service.ListPendingAsync(ctx.Guild.Id);

            if (pending.Count == 0)
            {
                await Response().Error(strs.banappeal_none_pending).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Pending Ban Appeals");

            foreach (var a in pending)
            {
                eb.AddField($"#{a.Id} - User {a.UserId}",
                    $"**Appeal:** {a.AppealText}\n" +
                    $"**Reason:** {(string.IsNullOrEmpty(a.Reason) ? "None" : a.Reason)}\n" +
                    $"**Submitted:** {a.SubmittedAt:yyyy-MM-dd HH:mm UTC}",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task BanAppealApprove(int id)
        {
            if (await _service.ReviewAppealAsync(ctx.Guild.Id, id, "Approved", ctx.User.Id))
                await Response().Confirm(strs.banappeal_approved(id)).SendAsync();
            else
                await Response().Error(strs.banappeal_not_found).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task BanAppealDeny(int id)
        {
            if (await _service.ReviewAppealAsync(ctx.Guild.Id, id, "Denied", ctx.User.Id))
                await Response().Confirm(strs.banappeal_denied(id)).SendAsync();
            else
                await Response().Error(strs.banappeal_not_found).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task BanAppealMessage([Leftover] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await Response().Error(strs.banappeal_message_empty).SendAsync();
                return;
            }

            if (await _service.SetAppealMessageAsync(ctx.Guild.Id, message))
                await Response().Confirm(strs.banappeal_message_set).SendAsync();
            else
                await Response().Error(strs.banappeal_not_configured).SendAsync();
        }
    }
}
