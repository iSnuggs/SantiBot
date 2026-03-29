#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("chprefix")]
    public partial class ChannelPrefixCommands : SantiModule<ChannelPrefixService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ChPrefix(ITextChannel channel, [Leftover] string prefix)
        {
            if (prefix.ToLowerInvariant() == "reset")
            {
                await _service.ResetPrefixAsync(ctx.Guild.Id, channel.Id);
                await Response().Confirm($"Prefix reset to default in {channel.Mention}.").SendAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(prefix) || prefix.Length > 10)
            {
                await Response().Error("Prefix must be 1-10 characters.").SendAsync();
                return;
            }

            await _service.SetPrefixAsync(ctx.Guild.Id, channel.Id, prefix);
            await Response().Confirm($"Custom prefix for {channel.Mention} set to `{prefix}`").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ChPrefixList()
        {
            var prefixes = await _service.ListPrefixesAsync(ctx.Guild.Id);
            if (prefixes.Count == 0)
            {
                await Response().Error("No custom channel prefixes set.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Channel Prefixes")
                .WithDescription(string.Join("\n", prefixes.Select(p => $"• <#{p.ChannelId}> → `{p.Prefix}`")))
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }
    }
}
