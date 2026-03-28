namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("serverstats")]
    [Name("MessageStats")]
    public partial class MessageStatsCommands : SantiModule<MessageStatsService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ServerStats(int days = 7)
        {
            if (days < 1 || days > 365)
            {
                await Response().Error(strs.stats_invalid_days).SendAsync();
                return;
            }

            var topUsers = await _service.GetTopUsersAsync(ctx.Guild.Id, days);
            var topChannels = await _service.GetTopChannelsAsync(ctx.Guild.Id, days);

            if (topUsers.Count == 0 && topChannels.Count == 0)
            {
                await Response().Error(strs.stats_no_data).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Server Stats (Last {days} days)");

            if (topUsers.Count > 0)
            {
                var userLines = topUsers.Select((u, i) =>
                    $"`{i + 1}.` <@{u.UserId}> — **{u.TotalMessages:N0}** messages");
                eb.AddField("Top Users", string.Join('\n', userLines));
            }

            if (topChannels.Count > 0)
            {
                var channelLines = topChannels.Select((c, i) =>
                    $"`{i + 1}.` <#{c.ChannelId}> — **{c.TotalMessages:N0}** messages");
                eb.AddField("Top Channels", string.Join('\n', channelLines));
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ServerStats(IUser user)
        {
            var stats = await _service.GetUserStatsAsync(ctx.Guild.Id, user.Id);
            if (stats is null)
            {
                await Response().Error(strs.stats_no_user_data(user.Mention)).SendAsync();
                return;
            }

            var (totalMessages, activeDays, topChannelId) = stats.Value;

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Stats for {user}")
                .AddField("Total Messages", $"**{totalMessages:N0}**", true)
                .AddField("Active Days", $"**{activeDays}**", true)
                .AddField("Most Active Channel", $"<#{topChannelId}>", true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
