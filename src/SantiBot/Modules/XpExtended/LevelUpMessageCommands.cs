#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("LevelMsg")]
    [Group("lvlmsg")]
    public partial class LevelUpMessageCommands : SantiModule<LevelUpMessageService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LvlmsgSet([Leftover] string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                await Response().Error("Provide a message template! Placeholders: {user}, {level}, {guild}").SendAsync();
                return;
            }

            await _service.SetMessageAsync(ctx.Guild.Id, template);
            var preview = _service.FormatMessage(template, ctx.User.ToString(), 5, ctx.Guild.Name);
            await Response().Confirm($"Level-up message set!\n**Preview:** {preview}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LvlmsgChannel(ITextChannel channel)
        {
            await _service.SetChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm($"Level-up messages will be sent to {channel.Mention}!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LvlmsgOff()
        {
            await _service.DisableAsync(ctx.Guild.Id);
            await Response().Confirm("Level-up messages disabled.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task LvlmsgInfo()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config is null || !config.Enabled)
            {
                await Response().Confirm("Level-up messages are not configured.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F389 Level-Up Message Config")
                .AddField("Template", config.MessageTemplate)
                .AddField("Channel", config.ChannelId != 0 ? $"<#{config.ChannelId}>" : "Same channel", true)
                .AddField("Enabled", config.Enabled.ToString(), true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
