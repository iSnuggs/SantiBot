namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("starboard")]
    [Name("Starboard")]
    public partial class StarboardCommands : SantiModule<StarboardService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task StarboardChannel(ITextChannel? channel = null)
        {
            if (channel is null)
            {
                await _service.DisableAsync(ctx.Guild.Id);
                await Response().Confirm(strs.starboard_disabled).SendAsync();
                return;
            }

            await _service.SetStarboardChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.starboard_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task StarboardThreshold(int threshold)
        {
            if (threshold < 1 || threshold > 50)
            {
                await Response().Error(strs.starboard_threshold_invalid).SendAsync();
                return;
            }

            await _service.SetThresholdAsync(ctx.Guild.Id, threshold);
            await Response().Confirm(strs.starboard_threshold_set(threshold)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task StarboardSelfStar(bool allow)
        {
            await _service.SetSelfStarAsync(ctx.Guild.Id, allow);
            await Response().Confirm(allow
                ? strs.starboard_selfstar_enabled
                : strs.starboard_selfstar_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task StarboardInfo()
        {
            var settings = _service.GetSettings(ctx.Guild.Id);
            if (settings is null)
            {
                await Response().Error(strs.starboard_not_configured).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Starboard Settings")
                .AddField("Channel", $"<#{settings.StarboardChannelId}>", true)
                .AddField("Threshold", settings.StarThreshold.ToString(), true)
                .AddField("Emoji", settings.StarEmoji, true)
                .AddField("Self-Star", settings.AllowSelfStar ? "Allowed" : "Not Allowed", true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
