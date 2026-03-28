namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Name("ThreadArchive")]
    [Group("threadarchive")]
    public partial class ThreadArchiveCommands : SantiModule<ThreadArchiveService>
    {
        [Cmd]
        [UserPerm(ChannelPermission.ManageThreads)]
        [BotPerm(ChannelPermission.ManageThreads)]
        [Priority(2)]
        public async Task ThreadArchive(ITextChannel channel, int minutes)
        {
            if (minutes < 60 || minutes > 10080) // 1 hour to 7 days
            {
                await Response().Error(strs.threadarchive_invalid_time).SendAsync();
                return;
            }

            await _service.SetArchiveTimeAsync(ctx.Guild.Id, channel.Id, minutes);
            await Response()
                .Confirm(strs.threadarchive_set(channel.Mention, minutes))
                .SendAsync();
        }

        [Cmd]
        [UserPerm(ChannelPermission.ManageThreads)]
        [BotPerm(ChannelPermission.ManageThreads)]
        [Priority(1)]
        public async Task ThreadArchive(ITextChannel channel, [Leftover] string mode)
        {
            switch (mode?.ToLowerInvariant())
            {
                case "keepalive":
                    await _service.SetKeepAliveAsync(ctx.Guild.Id, channel.Id);
                    await Response()
                        .Confirm(strs.threadarchive_keepalive(channel.Mention))
                        .SendAsync();
                    break;

                case "default":
                    var removed = await _service.RemoveConfigAsync(ctx.Guild.Id, channel.Id);
                    if (removed)
                        await Response()
                            .Confirm(strs.threadarchive_reset(channel.Mention))
                            .SendAsync();
                    else
                        await Response()
                            .Error(strs.threadarchive_no_config)
                            .SendAsync();
                    break;

                default:
                    await Response().Error(strs.threadarchive_invalid_mode).SendAsync();
                    break;
            }
        }

        [Cmd]
        [Priority(0)]
        public async Task ThreadArchive()
        {
            // List all configs
            var configs = await _service.GetConfigsAsync(ctx.Guild.Id);

            if (configs.Count == 0)
            {
                await Response().Error(strs.threadarchive_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.threadarchive_list));

            foreach (var config in configs)
            {
                var mode = config.KeepAlive
                    ? "Keep Alive"
                    : $"Archive after {config.ArchiveAfterMinutes} min";

                eb.AddField($"<#{config.ChannelId}>", mode, true);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
