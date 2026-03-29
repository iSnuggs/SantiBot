#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("agegate")]
    public partial class ContentAgeGateCommands : SantiModule<ContentAgeGateService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AgeGate(ITextChannel channel, IRole role)
        {
            await _service.SetGateAsync(ctx.Guild.Id, channel.Id, role.Id);
            await Response().Confirm($"Age gate set on {channel.Mention}.\nRequired role: **{role.Name}**").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AgeGateRemove(ITextChannel channel)
        {
            if (await _service.RemoveGateAsync(ctx.Guild.Id, channel.Id))
                await Response().Confirm($"Age gate removed from {channel.Mention}.").SendAsync();
            else
                await Response().Error("No age gate on that channel.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AgeGateList()
        {
            var gates = await _service.ListGatesAsync(ctx.Guild.Id);
            if (gates.Count == 0)
            {
                await Response().Error("No age gates configured.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Content Age Gates")
                .WithDescription(string.Join("\n", gates.Select(g => $"• <#{g.ChannelId}> → <@&{g.RequiredRoleId}>")))
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }
    }
}
