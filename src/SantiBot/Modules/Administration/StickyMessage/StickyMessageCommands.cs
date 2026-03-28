namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("sticky")]
    [Name("StickyMessage")]
    public partial class StickyMessageCommands : SantiModule<StickyMessageService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task Sticky(ITextChannel channel, [Leftover] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await Response().Error(strs.sticky_no_message).SendAsync();
                return;
            }

            if (message.Length > 2000)
            {
                await Response().Error(strs.sticky_too_long).SendAsync();
                return;
            }

            await _service.SetStickyAsync(ctx.Guild.Id, channel.Id, message);
            await Response().Confirm(strs.sticky_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task StickyRemove(ITextChannel channel)
        {
            var removed = await _service.RemoveStickyAsync(ctx.Guild.Id, channel.Id);
            if (removed)
                await Response().Confirm(strs.sticky_removed(channel.Mention)).SendAsync();
            else
                await Response().Error(strs.sticky_not_found(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task StickyList()
        {
            var stickies = await _service.GetAllStickiesAsync(ctx.Guild.Id);
            if (stickies.Count == 0)
            {
                await Response().Error(strs.sticky_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Sticky Messages");

            foreach (var s in stickies)
            {
                var preview = s.Content.Length > 100
                    ? s.Content[..100] + "..."
                    : s.Content;
                eb.AddField($"<#{s.ChannelId}>", preview);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
