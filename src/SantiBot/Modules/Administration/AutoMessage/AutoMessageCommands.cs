#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class AutoMessageCommands : SantiModule<AutoMessageService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ScheduleMessage(ITextChannel channel, string timeStr, [Leftover] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                await Response().Error(strs.schedule_no_content).SendAsync();
                return;
            }

            if (!TryParseTimeSpan(timeStr, out var delay) || delay.TotalMinutes < 1)
            {
                await Response().Error(strs.schedule_invalid_time).SendAsync();
                return;
            }

            var sendAt = DateTime.UtcNow + delay;
            var msg = await _service.ScheduleOneTimeAsync(ctx.Guild.Id, channel.Id, ctx.User.Id, sendAt, content);

            await Response().Confirm(strs.schedule_created(msg.Id, channel.Mention, sendAt.ToString("yyyy-MM-dd HH:mm UTC"))).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ScheduleRecurring(ITextChannel channel, string intervalStr, [Leftover] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                await Response().Error(strs.schedule_no_content).SendAsync();
                return;
            }

            if (!TryParseTimeSpan(intervalStr, out var interval) || interval.TotalMinutes < 5)
            {
                await Response().Error(strs.schedule_invalid_interval).SendAsync();
                return;
            }

            var msg = await _service.ScheduleRecurringAsync(ctx.Guild.Id, channel.Id, ctx.User.Id, interval, content);

            await Response().Confirm(strs.schedule_recurring_created(msg.Id, channel.Mention, FormatTimeSpan(interval))).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ScheduleCancel(int messageId)
        {
            var success = await _service.CancelMessageAsync(ctx.Guild.Id, messageId);

            if (success)
                await Response().Confirm(strs.schedule_cancelled(messageId)).SendAsync();
            else
                await Response().Error(strs.schedule_not_found(messageId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ScheduleList()
        {
            var messages = await _service.GetActiveMessagesAsync(ctx.Guild.Id);

            if (messages.Count == 0)
            {
                await Response().Confirm(strs.schedule_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Scheduled Messages")
                .WithOkColor();

            foreach (var msg in messages.Take(15))
            {
                var type = msg.IsRecurring ? $"🔁 Every {FormatTimeSpan(msg.Interval.Value)}" : "📩 One-time";
                embed.AddField(
                    $"#{msg.Id} — {type}",
                    $"Channel: <#{msg.ChannelId}>\n" +
                    $"Next: {msg.ScheduledAt:yyyy-MM-dd HH:mm} UTC\n" +
                    $"Content: {(msg.Content.Length > 60 ? msg.Content[..60] + "..." : msg.Content)}",
                    false);
            }

            await Response().Embed(embed).SendAsync();
        }

        private static bool TryParseTimeSpan(string input, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrEmpty(input))
                return false;

            input = input.ToLowerInvariant().Trim();
            var totalMinutes = 0d;

            // Parse formats like "1h30m", "2d", "30m", "1h", "24h"
            var i = 0;
            while (i < input.Length)
            {
                var numStart = i;
                while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.'))
                    i++;

                if (i == numStart || i >= input.Length)
                    break;

                if (!double.TryParse(input[numStart..i], out var num))
                    break;

                var unit = input[i];
                i++;

                totalMinutes += unit switch
                {
                    'd' => num * 1440,
                    'h' => num * 60,
                    'm' => num,
                    's' => num / 60,
                    _ => 0,
                };
            }

            if (totalMinutes <= 0)
                return false;

            result = TimeSpan.FromMinutes(totalMinutes);
            return true;
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{(int)ts.TotalMinutes}m";
        }
    }
}
