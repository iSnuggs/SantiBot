#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("slowsched")]
    public partial class SlowmodeSchedulerCommands : SantiModule<SlowmodeSchedulerService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task SlowSched(ITextChannel channel, int seconds, string startTime, string endTime)
        {
            if (seconds < 0 || seconds > 21600)
            {
                await Response().Error("Slowmode must be between 0 and 21600 seconds.").SendAsync();
                return;
            }

            if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
            {
                await Response().Error("Invalid time format. Use HH:mm (e.g., 09:00, 22:00).").SendAsync();
                return;
            }

            var sched = await _service.AddScheduleAsync(ctx.Guild.Id, channel.Id, seconds, start, end);
            await Response().Confirm($"Slowmode schedule **#{sched.Id}** created.\n{channel.Mention}: **{seconds}s** slowmode from **{start:hh\\:mm}** to **{end:hh\\:mm}** UTC").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task SlowSchedList()
        {
            var schedules = await _service.ListSchedulesAsync(ctx.Guild.Id);
            if (schedules.Count == 0)
            {
                await Response().Error("No slowmode schedules configured.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Slowmode Schedules")
                .WithOkColor();

            foreach (var s in schedules)
            {
                embed.AddField($"#{s.Id} {(s.IsEnabled ? "✅" : "❌")}",
                    $"<#{s.ChannelId}> | **{s.SlowmodeSeconds}s** | {s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm} UTC");
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task SlowSchedDel(int id)
        {
            if (await _service.RemoveScheduleAsync(ctx.Guild.Id, id))
                await Response().Confirm($"Slowmode schedule **#{id}** deleted.").SendAsync();
            else
                await Response().Error("Schedule not found.").SendAsync();
        }
    }
}
