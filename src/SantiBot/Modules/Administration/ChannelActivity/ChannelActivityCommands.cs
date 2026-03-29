#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("chactivity")]
    public partial class ChannelActivityCommands : SantiModule<ChannelActivityService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ChActivity(int days = 7)
        {
            if (days < 1 || days > 90)
            {
                await Response().Error("Days must be between 1 and 90.").SendAsync();
                return;
            }

            var activity = await _service.GetActivityAsync(ctx.Guild.Id, days);
            if (activity.Count == 0)
            {
                await Response().Error("No channel activity data available yet. Data is collected as messages are sent.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle($"Channel Activity (Last {days} Days)")
                .WithOkColor();

            var sb = new System.Text.StringBuilder();
            var maxMessages = activity.First().TotalMessages;

            foreach (var (channelId, total) in activity.Take(20))
            {
                var barLength = maxMessages > 0 ? (int)(((double)total / maxMessages) * 15) : 0;
                var bar = new string('\u2588', Math.Max(barLength, 1));
                sb.AppendLine($"<#{channelId}> {bar} **{total:N0}**");
            }

            embed.WithDescription(sb.ToString());

            if (activity.Count > 20)
                embed.WithFooter($"...and {activity.Count - 20} more channels");

            await Response().Embed(embed).SendAsync();
        }
    }
}
