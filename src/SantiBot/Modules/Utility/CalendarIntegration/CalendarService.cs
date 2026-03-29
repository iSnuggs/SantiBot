#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.CalendarIntegration;

public sealed class CalendarService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly ShardData _shardData;

    public CalendarService(
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
                await CheckReminders();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error checking calendar reminders");
            }
        }
    }

    private async Task CheckReminders()
    {
        await using var uow = _db.GetDbContext();
        var now = DateTime.UtcNow;
        var events = await uow.GetTable<CalendarEvent>()
            .Where(x => !x.ReminderSent &&
                        x.ReminderMinutesBefore != null &&
                        Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var ev in events)
        {
            var reminderTime = ev.EventDate.AddMinutes(-ev.ReminderMinutesBefore.Value);
            if (now < reminderTime) continue;

            // Send DM reminder to creator
            try
            {
                var user = await _client.GetUserAsync(ev.CreatorId);
                if (user is not null)
                {
                    var dmChannel = await user.CreateDMChannelAsync();
                    await _sender.Response(dmChannel)
                        .Embed(_sender.CreateEmbed()
                            .WithTitle("Calendar Reminder")
                            .WithDescription(
                                $"**{ev.Title}**\n{ev.Description ?? ""}\nStarts: <t:{new DateTimeOffset(ev.EventDate).ToUnixTimeSeconds()}:F>")
                            .WithOkColor())
                        .SendAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Could not DM calendar reminder to user {UserId}", ev.CreatorId);
            }

            await uow.GetTable<CalendarEvent>()
                .Where(x => x.Id == ev.Id)
                .UpdateAsync(x => new CalendarEvent { ReminderSent = true });
        }
    }

    public async Task<int> AddEventAsync(ulong guildId, ulong creatorId, DateTime eventDate,
        string title, string description = null)
    {
        await using var uow = _db.GetDbContext();
        var ev = await uow.GetTable<CalendarEvent>()
            .InsertWithOutputAsync(() => new CalendarEvent
            {
                GuildId = guildId,
                CreatorId = creatorId,
                EventDate = eventDate,
                Title = title,
                Description = description
            });
        return ev.Id;
    }

    public async Task<List<CalendarEvent>> ListEventsAsync(ulong guildId, int? month = null)
    {
        await using var uow = _db.GetDbContext();
        var query = uow.GetTable<CalendarEvent>()
            .Where(x => x.GuildId == guildId && x.EventDate >= DateTime.UtcNow);

        if (month.HasValue)
            query = query.Where(x => x.EventDate.Month == month.Value);

        return await query.OrderBy(x => x.EventDate).Take(25).ToListAsyncLinqToDB();
    }

    public async Task<List<CalendarEvent>> TodayEventsAsync(ulong guildId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<CalendarEvent>()
            .Where(x => x.GuildId == guildId && x.EventDate >= today && x.EventDate < tomorrow)
            .OrderBy(x => x.EventDate)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> RemoveEventAsync(ulong guildId, int eventId)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<CalendarEvent>()
            .Where(x => x.GuildId == guildId && x.Id == eventId)
            .DeleteAsync();
        return count > 0;
    }

    public async Task<bool> SetReminderAsync(ulong guildId, int eventId, int minutesBefore)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<CalendarEvent>()
            .Where(x => x.GuildId == guildId && x.Id == eventId)
            .UpdateAsync(x => new CalendarEvent
            {
                ReminderMinutesBefore = minutesBefore,
                ReminderSent = false
            });
        return count > 0;
    }
}
