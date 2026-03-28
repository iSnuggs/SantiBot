using SantiBot.Common.TypeReaders.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("schedtimeout")]
    [Name("TimeoutScheduler")]
    public partial class TimeoutSchedulerCommands : SantiModule<TimeoutSchedulerService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task SchedTimeout(IGuildUser user, ParsedTimespan duration, ParsedTimespan delay, [Leftover] string reason = "")
        {
            if (duration.Time.TotalMinutes < 1 || duration.Time.TotalDays > 28)
            {
                await Response().Error(strs.schedtimeout_duration_invalid).SendAsync();
                return;
            }

            if (delay.Time.TotalMinutes < 1 || delay.Time.TotalDays > 30)
            {
                await Response().Error(strs.schedtimeout_delay_invalid).SendAsync();
                return;
            }

            var scheduledFor = DateTime.UtcNow + delay.Time;
            var durationMinutes = (int)duration.Time.TotalMinutes;

            var timeout = await _service.ScheduleAsync(
                ctx.Guild.Id, user.Id, ctx.User.Id, scheduledFor, durationMinutes, reason);

            await Response().Confirm(strs.schedtimeout_scheduled(
                user.Mention,
                durationMinutes,
                scheduledFor.ToString("yyyy-MM-dd HH:mm UTC"),
                timeout.Id)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task SchedTimeoutList()
        {
            var pending = await _service.ListAsync(ctx.Guild.Id);

            if (pending.Count == 0)
            {
                await Response().Error(strs.schedtimeout_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Scheduled Timeouts");

            foreach (var t in pending)
            {
                eb.AddField($"#{t.Id} - <@{t.TargetUserId}>",
                    $"**When:** {t.ScheduledFor:yyyy-MM-dd HH:mm UTC}\n" +
                    $"**Duration:** {t.DurationMinutes}m\n" +
                    $"**Reason:** {(string.IsNullOrEmpty(t.Reason) ? "None" : t.Reason)}\n" +
                    $"**By:** <@{t.ModeratorUserId}>",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task SchedTimeoutCancel(int id)
        {
            if (await _service.CancelAsync(ctx.Guild.Id, id))
                await Response().Confirm(strs.schedtimeout_cancelled(id)).SendAsync();
            else
                await Response().Error(strs.schedtimeout_not_found).SendAsync();
        }
    }
}
