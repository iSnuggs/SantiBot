using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class EventSchedulerService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;
    private readonly IMessageSenderService _sender;
    private readonly IBotStrings _strings;
    private readonly ILocalization _localization;

    public EventSchedulerService(
        DbService db,
        DiscordSocketClient client,
        IBotCreds creds,
        IMessageSenderService sender,
        IBotStrings strings,
        ILocalization localization)
    {
        _db = db;
        _client = client;
        _creds = creds;
        _sender = sender;
        _strings = strings;
        _localization = localization;
    }

    public async Task OnReadyAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await CheckRemindersAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error checking event reminders");
            }
        }
    }

    private async Task CheckRemindersAsync()
    {
        await using var ctx = _db.GetDbContext();

        var cutoff = DateTime.UtcNow.AddMinutes(15);
        var events = await ctx.GetTable<ScheduledEvent>()
            .Where(x => !x.ReminderSent
                         && x.StartsAt <= cutoff
                         && x.StartsAt > DateTime.UtcNow
                         && Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var ev in events)
        {
            try
            {
                await SendReminderAsync(ev);

                // Mark as sent
                await ctx.GetTable<ScheduledEvent>()
                    .Where(x => x.Id == ev.Id)
                    .Set(x => x.ReminderSent, true)
                    .UpdateAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send reminder for event {EventId}", ev.Id);
            }
        }

        // Clean up past events (more than 1 hour after start)
        await ctx.GetTable<ScheduledEvent>()
            .Where(x => x.StartsAt < DateTime.UtcNow.AddHours(-1)
                         && Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .DeleteAsync();
    }

    private async Task SendReminderAsync(ScheduledEvent ev)
    {
        var guild = _client.GetGuild(ev.GuildId);
        if (guild is null)
            return;

        var channelId = ev.ChannelId ?? 0;
        var ch = guild.GetTextChannel(channelId);

        // Fall back to the system channel
        ch ??= guild.SystemChannel;

        if (ch is null)
            return;

        var culture = _localization.GetCultureInfo(ev.GuildId);

        var pingText = ev.PingRoleId is ulong roleId
            ? $"<@&{roleId}> "
            : "";

        var rsvpList = string.IsNullOrWhiteSpace(ev.RsvpUserIds)
            ? "None yet"
            : string.Join(", ", ev.RsvpUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => $"<@{id}>"));

        var eb = _sender.CreateEmbed(ev.GuildId)
            .WithOkColor()
            .WithTitle($"Event Starting Soon: {ev.Title}")
            .WithDescription(ev.Description)
            .AddField("Starts At", TimestampTag.FromDateTime(ev.StartsAt).ToString(), true)
            .AddField("RSVPs", rsvpList, true)
            .WithFooter($"Event #{ev.Id}");

        await _sender.Response(ch).Text(pingText).Embed(eb).SendAsync();
    }

    public async Task<ScheduledEvent> CreateEventAsync(
        ulong guildId,
        ulong creatorId,
        string title,
        string description,
        DateTime startsAt,
        ulong? channelId)
    {
        await using var ctx = _db.GetDbContext();

        var ev = await ctx.GetTable<ScheduledEvent>()
            .InsertWithOutputAsync(() => new ScheduledEvent
            {
                GuildId = guildId,
                CreatorUserId = creatorId,
                Title = title,
                Description = description,
                StartsAt = startsAt,
                ChannelId = channelId,
            });

        return ev;
    }

    public async Task<bool> RsvpAsync(ulong guildId, int eventId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var ev = await ctx.GetTable<ScheduledEvent>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Id == eventId);

        if (ev is null)
            return false;

        var ids = ev.RsvpUserIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        var userIdStr = userId.ToString();

        if (ids.Contains(userIdStr))
            return true; // Already RSVP'd

        ids.Add(userIdStr);

        await ctx.GetTable<ScheduledEvent>()
            .Where(x => x.Id == eventId)
            .Set(x => x.RsvpUserIds, string.Join(",", ids))
            .UpdateAsync();

        return true;
    }

    public async Task<IReadOnlyList<ScheduledEvent>> GetUpcomingEventsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<ScheduledEvent>()
            .Where(x => x.GuildId == guildId && x.StartsAt > DateTime.UtcNow)
            .OrderBy(x => x.StartsAt)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> CancelEventAsync(ulong guildId, int eventId, ulong userId, bool isAdmin)
    {
        await using var ctx = _db.GetDbContext();

        var ev = await ctx.GetTable<ScheduledEvent>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Id == eventId);

        if (ev is null)
            return false;

        // Only creator or admin can cancel
        if (ev.CreatorUserId != userId && !isAdmin)
            return false;

        await ctx.GetTable<ScheduledEvent>()
            .Where(x => x.Id == eventId)
            .DeleteAsync();

        return true;
    }

    public async Task<bool> SetPingRoleAsync(ulong guildId, int eventId, ulong roleId)
    {
        await using var ctx = _db.GetDbContext();

        var updated = await ctx.GetTable<ScheduledEvent>()
            .Where(x => x.GuildId == guildId && x.Id == eventId)
            .Set(x => x.PingRoleId, roleId)
            .UpdateAsync();

        return updated > 0;
    }
}
