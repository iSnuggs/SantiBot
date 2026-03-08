#nullable disable
namespace NadekoBot.Modules.Administration;

public enum List
{
    List = 0,
    Ls = 0
}

public enum Server
{
    Server
}

public enum _Channel
{
    Channel,
    Ch,
    Chnl,
    Chan
}

public enum State
{
    Enable,
    Disable,
    Inherit
}

public partial class Administration
{
    [Group]
    public partial class DelMsgOnCmdCommands(AdministrationService service) : NadekoModule<AdministrationService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageMessages)]
        [Priority(2)]
        public async Task Delmsgoncmd(List _)
        {
            var guild = (SocketGuild)ctx.Guild;
            var (enabled, channels) = await service.GetDelMsgOnCmdData(ctx.Guild.Id);

            var embed = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.server_delmsgoncmd))
                .WithDescription(enabled ? "✅" : "❌");

            var str = string.Join("\n",
                channels.Select(x =>
                {
                    var ch = guild.GetChannel(x.ChannelId)?.ToString() ?? x.ChannelId.ToString();
                    var prefixSign = x.State ? "✅ " : "❌ ";
                    return prefixSign + ch;
                }));

            if (string.IsNullOrWhiteSpace(str))
                str = "-";

            embed.AddField(GetText(strs.channel_delmsgoncmd), str);

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageMessages)]
        [Priority(1)]
        public async Task Delmsgoncmd(Server _ = Server.Server)
        {
            var enabled = await service.ToggleDelMsgOnCmd(ctx.Guild.Id);
            if (enabled)
            {
                await Response().Confirm(strs.delmsg_on).SendAsync();
            }
            else
            {
                await Response().Confirm(strs.delmsg_off).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageMessages)]
        [Priority(0)]
        public Task Delmsgoncmd(_Channel _, State s, ITextChannel ch)
            => Delmsgoncmd(_, s, ch.Id);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageMessages)]
        [Priority(1)]
        public async Task Delmsgoncmd(_Channel _, State s, ulong? chId = null)
        {
            var actualChId = chId ?? ctx.Channel.Id;
            await service.SetDelMsgOnCmdState(ctx.Guild.Id, actualChId, s);

            if (s == State.Disable)
                await Response().Confirm(strs.delmsg_channel_off).SendAsync();
            else if (s == State.Enable)
                await Response().Confirm(strs.delmsg_channel_on).SendAsync();
            else
                await Response().Confirm(strs.delmsg_channel_inherit).SendAsync();
        }
    }
}
