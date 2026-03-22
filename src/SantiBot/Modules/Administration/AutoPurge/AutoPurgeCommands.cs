namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("autopurge")]
    [Name("AutoPurge")]
    public partial class AutoPurgeCommands : SantiModule<AutoPurgeService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AutoPurgeSet(ITextChannel channel, int intervalHours, int maxAgeHours)
        {
            if (intervalHours < 1 || intervalHours > 720)
            {
                await Response().Error(strs.autopurge_interval_invalid).SendAsync();
                return;
            }

            if (maxAgeHours < 1 || maxAgeHours > 336) // max 14 days
            {
                await Response().Error(strs.autopurge_age_invalid).SendAsync();
                return;
            }

            await _service.SetAutoPurgeAsync(ctx.Guild.Id, channel.Id, intervalHours, maxAgeHours);
            await Response().Confirm(strs.autopurge_set(channel.Mention, intervalHours, maxAgeHours)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AutoPurgeRemove(ITextChannel channel)
        {
            if (await _service.RemoveAutoPurgeAsync(ctx.Guild.Id, channel.Id))
                await Response().Confirm(strs.autopurge_removed(channel.Mention)).SendAsync();
            else
                await Response().Error(strs.autopurge_not_found).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AutoPurgeList()
        {
            var configs = await _service.GetAutoPurgesAsync(ctx.Guild.Id);

            if (configs.Count == 0)
            {
                await Response().Error(strs.autopurge_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Auto Purge Configurations");

            foreach (var c in configs)
            {
                eb.AddField($"<#{c.ChannelId}>",
                    $"Every **{c.IntervalHours}h** — delete messages older than **{c.MaxMessageAgeHours}h**",
                    true);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
