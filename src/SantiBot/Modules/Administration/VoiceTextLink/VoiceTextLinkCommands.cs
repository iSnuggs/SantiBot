#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class VoiceTextLinkCommands : SantiModule<VoiceTextLinkService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task VtLink(IVoiceChannel voiceChannel, ITextChannel textChannel)
        {
            await _service.AddLinkAsync(ctx.Guild.Id, voiceChannel.Id, textChannel.Id);
            await Response().Confirm(strs.vtlink_added(voiceChannel.Name, textChannel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task VtUnlink(IVoiceChannel voiceChannel)
        {
            var removed = await _service.RemoveLinkAsync(ctx.Guild.Id, voiceChannel.Id);

            if (removed)
                await Response().Confirm(strs.vtlink_removed(voiceChannel.Name)).SendAsync();
            else
                await Response().Error(strs.vtlink_not_found(voiceChannel.Name)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task VtList()
        {
            var links = await _service.GetLinksAsync(ctx.Guild.Id);

            if (links.Count == 0)
            {
                await Response().Confirm(strs.vtlink_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Voice-Text Links")
                .WithOkColor();

            foreach (var link in links)
                embed.AddField($"🔊 <#{link.VoiceChannelId}>", $"→ <#{link.TextChannelId}>", true);

            await Response().Embed(embed).SendAsync();
        }
    }
}
