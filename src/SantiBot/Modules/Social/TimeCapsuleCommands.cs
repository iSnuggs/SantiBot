#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("TimeCapsule")]
    [Group("timecapsule")]
    public partial class TimeCapsuleCommands : SantiModule<TimeCapsuleService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Timecapsule(string duration, [Leftover] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await Response().Error("Usage: `.timecapsule <duration> <message>` (e.g., `.timecapsule 30d Remember to...`)").SendAsync();
                return;
            }

            var timespan = TimeCapsuleService.ParseDuration(duration);
            if (timespan is null || timespan.Value.TotalMinutes < 1)
            {
                await Response().Error("Invalid duration! Use: 1h, 7d, 4w, etc.").SendAsync();
                return;
            }

            if (timespan.Value.TotalDays > 365)
            {
                await Response().Error("Maximum duration is 365 days!").SendAsync();
                return;
            }

            if (message.Length > 500)
            {
                await Response().Error("Message must be 500 characters or less!").SendAsync();
                return;
            }

            await _service.CreateCapsuleAsync(ctx.Guild.Id, ctx.User.Id, message, timespan.Value);
            var deliverDate = DateTime.UtcNow.Add(timespan.Value);
            await Response().Confirm(
                $"\U0001F4E6 Time capsule sealed! It will be delivered to your DMs on **{deliverDate:MMM dd, yyyy}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TimecapsuleList()
        {
            var capsules = await _service.GetCapsulesAsync(ctx.User.Id);

            if (capsules.Count == 0)
            {
                await Response().Confirm("You have no pending time capsules!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < capsules.Count; i++)
            {
                var c = capsules[i];
                var preview = c.Message.Length > 30 ? c.Message[..30] + "..." : c.Message;
                sb.AppendLine($"**#{i + 1}** Delivers: {c.DeliverAt:MMM dd, yyyy} — \"{preview}\"");
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F4E6 Your Time Capsules")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
