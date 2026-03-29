#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("autoarchive")]
    public partial class AutoArchiveCommands : SantiModule<AutoArchiveService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task AutoArchive(int days)
        {
            if (days < 1 || days > 365)
            {
                await Response().Error("Days must be between 1 and 365.").SendAsync();
                return;
            }

            await _service.SetConfigAsync(ctx.Guild.Id, days);
            await Response().Confirm($"Auto-archive enabled. Channels inactive for **{days} days** will be archived.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task AutoArchiveExclude(ITextChannel channel)
        {
            await _service.AddExclusionAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm($"{channel.Mention} excluded from auto-archive.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task AutoArchiveList()
        {
            var (config, exclusions) = await _service.GetInfoAsync(ctx.Guild.Id);
            if (config is null)
            {
                await Response().Error("Auto-archive is not configured.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Auto-Archive Configuration")
                .AddField("Status", config.IsEnabled ? "Enabled" : "Disabled", true)
                .AddField("Inactive Days", config.InactiveDays.ToString(), true)
                .WithOkColor();

            if (exclusions.Count > 0)
                embed.AddField("Excluded Channels", string.Join("\n", exclusions.Select(e => $"<#{e.ChannelId}>")));

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task AutoArchiveOff()
        {
            await _service.DisableAsync(ctx.Guild.Id);
            await Response().Confirm("Auto-archive disabled.").SendAsync();
        }
    }
}
