namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("ScheduledTasks")]
    [Group("scheduledtask")]
    public partial class ScheduledTaskCommands : SantiModule<DashboardApi.ScheduledTasks.ScheduledTaskService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ScheduledTaskCreate(string schedule, [Leftover] string message)
        {
            // schedule format: every:60 (minutes), daily:14:00, hourly, weekly:Monday:14:00
            var channel = (ITextChannel)ctx.Channel;
            var id = await _service.CreateTaskAsync(ctx.Guild.Id, channel.Id, message, schedule);
            await Response()
                .Confirm($"Scheduled task #{id} created with schedule `{schedule}` in {channel.Mention}")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ScheduledTaskList()
        {
            var tasks = await _service.ListTasksAsync(ctx.Guild.Id);
            if (tasks.Count == 0)
            {
                await Response().Error("No scheduled tasks.").SendAsync();
                return;
            }

            var desc = string.Join("\n", tasks.Select(t =>
            {
                var nextRun = t.NextRun.HasValue
                    ? $"<t:{new DateTimeOffset(t.NextRun.Value).ToUnixTimeSeconds()}:R>"
                    : "N/A";
                return $"`#{t.Id}` [{(t.IsEnabled ? "ON" : "OFF")}] `{t.CronExpression}` -> <#{t.ChannelId}> (Next: {nextRun})\n  {t.Message.TrimTo(80)}";
            }));

            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Scheduled Tasks").WithDescription(desc))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ScheduledTaskDelete(int id)
        {
            var success = await _service.DeleteTaskAsync(ctx.Guild.Id, id);
            if (success)
                await Response().Confirm($"Scheduled task #{id} deleted.").SendAsync();
            else
                await Response().Error("Task not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ScheduledTaskToggle(int id)
        {
            var success = await _service.ToggleTaskAsync(ctx.Guild.Id, id);
            if (success)
                await Response().Confirm($"Scheduled task #{id} toggled.").SendAsync();
            else
                await Response().Error("Task not found.").SendAsync();
        }
    }
}
