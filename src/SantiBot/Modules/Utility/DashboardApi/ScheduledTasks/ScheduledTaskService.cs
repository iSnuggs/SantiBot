#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.DashboardApi.ScheduledTasks;

/// <summary>
/// Scheduled tasks manager.
/// API: GET/POST/PUT/DELETE /api/guild/{guildId}/scheduled-tasks
/// </summary>
public sealed class ScheduledTaskService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly ShardData _shardData;

    public ScheduledTaskService(
        DbService db,
        DiscordSocketClient client,
        IMessageSenderService sender,
        ShardData shardData)
    {
        _db = db;
        _client = client;
        _sender = sender;
        _shardData = shardData;
    }

    public async Task OnReadyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await ExecuteDueTasks();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error executing scheduled tasks");
            }
        }
    }

    private async Task ExecuteDueTasks()
    {
        var now = DateTime.UtcNow;
        await using var uow = _db.GetDbContext();
        var dueTasks = await uow.GetTable<ScheduledTask>()
            .Where(x => x.IsEnabled &&
                        x.NextRun != null &&
                        x.NextRun <= now &&
                        Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var task in dueTasks)
        {
            try
            {
                var guild = _client.GetGuild(task.GuildId);
                var ch = guild?.GetTextChannel(task.ChannelId);
                if (ch is null) continue;

                await ch.SendMessageAsync(task.Message);

                // Calculate next run from cron expression
                var nextRun = CalculateNextRun(task.CronExpression);
                await uow.GetTable<ScheduledTask>()
                    .Where(x => x.Id == task.Id)
                    .UpdateAsync(x => new ScheduledTask { NextRun = nextRun });
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error executing scheduled task {TaskId}", task.Id);
            }
        }
    }

    private static DateTime? CalculateNextRun(string cronExpression)
    {
        // Simple cron-like parsing: supports interval format like "every:60" (minutes)
        // or "daily:HH:mm" or "weekly:DayOfWeek:HH:mm"
        try
        {
            var parts = cronExpression.Split(':');
            return parts[0].ToLower() switch
            {
                "every" => DateTime.UtcNow.AddMinutes(int.Parse(parts[1])),
                "daily" => DateTime.UtcNow.Date.AddDays(1)
                    .Add(TimeSpan.Parse(parts[1])),
                "weekly" => DateTime.UtcNow.Date.AddDays(7)
                    .Add(parts.Length > 2 ? TimeSpan.Parse(parts[2]) : TimeSpan.Zero),
                "hourly" => DateTime.UtcNow.AddHours(1),
                _ => DateTime.UtcNow.AddHours(24)
            };
        }
        catch
        {
            return DateTime.UtcNow.AddHours(24);
        }
    }

    public async Task<int> CreateTaskAsync(ulong guildId, ulong channelId, string message, string cronExpression)
    {
        var nextRun = CalculateNextRun(cronExpression);
        await using var uow = _db.GetDbContext();
        var task = await uow.GetTable<ScheduledTask>()
            .InsertWithOutputAsync(() => new ScheduledTask
            {
                GuildId = guildId,
                ChannelId = channelId,
                Message = message,
                CronExpression = cronExpression,
                NextRun = nextRun,
                IsEnabled = true
            });
        return task.Id;
    }

    public async Task<List<ScheduledTask>> ListTasksAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<ScheduledTask>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> DeleteTaskAsync(ulong guildId, int taskId)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<ScheduledTask>()
            .Where(x => x.GuildId == guildId && x.Id == taskId)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<bool> ToggleTaskAsync(ulong guildId, int taskId)
    {
        await using var uow = _db.GetDbContext();
        var task = await uow.GetTable<ScheduledTask>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Id == taskId);
        if (task is null) return false;

        await uow.GetTable<ScheduledTask>()
            .Where(x => x.Id == taskId)
            .UpdateAsync(x => new ScheduledTask { IsEnabled = !task.IsEnabled });
        return true;
    }
}
