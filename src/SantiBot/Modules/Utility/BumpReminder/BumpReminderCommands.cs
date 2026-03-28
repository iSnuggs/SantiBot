namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("bumpreminder")]
    [Name("BumpReminder")]
    public partial class BumpReminderCommands : SantiModule<BumpReminderService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BumpReminder(ITextChannel channel)
        {
            await _service.SetChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.bump_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BumpReminderRole(IRole role)
        {
            await _service.SetRoleAsync(ctx.Guild.Id, role.Id);
            await Response().Confirm(strs.bump_role_set(role.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BumpReminderDisable()
        {
            await _service.DisableAsync(ctx.Guild.Id);
            await Response().Confirm(strs.bump_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BumpReminderStatus()
        {
            var config = _service.GetConfig(ctx.Guild.Id);
            if (config is null)
            {
                await Response().Error(strs.bump_not_configured).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Bump Reminder Settings")
                .AddField("Channel", $"<#{config.ChannelId}>", true)
                .AddField("Enabled", config.Enabled ? "Yes" : "No", true)
                .AddField("Ping Role", config.PingRoleId.HasValue ? $"<@&{config.PingRoleId.Value}>" : "None", true)
                .AddField("Interval", $"{config.IntervalMinutes} minutes", true)
                .AddField("Last Bump", config.LastBumpAt.HasValue
                    ? $"<t:{((DateTimeOffset)config.LastBumpAt.Value).ToUnixTimeSeconds()}:R>"
                    : "Never", true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
