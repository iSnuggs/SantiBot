using System.Globalization;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("EventScheduler")]
    [Group("event")]
    public partial class EventSchedulerCommands : SantiModule<EventSchedulerService>
    {
        [Cmd]
        public async Task EventCreate(string title, string description, [Leftover] string dateTimeStr)
        {
            if (!DateTime.TryParse(dateTimeStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var startsAt))
            {
                await Response().Error(strs.event_invalid_date).SendAsync();
                return;
            }

            if (startsAt <= DateTime.UtcNow)
            {
                await Response().Error(strs.event_past_date).SendAsync();
                return;
            }

            var ev = await _service.CreateEventAsync(
                ctx.Guild.Id,
                ctx.User.Id,
                title,
                description,
                startsAt,
                ctx.Channel.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.event_created))
                .WithDescription(ev.Title)
                .AddField("Description", ev.Description, false)
                .AddField("Starts At", TimestampTag.FromDateTime(ev.StartsAt).ToString(), true)
                .WithFooter($"Event #{ev.Id} — Use .event rsvp {ev.Id} to join!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        public async Task EventRsvp(int eventId)
        {
            var success = await _service.RsvpAsync(ctx.Guild.Id, eventId, ctx.User.Id);

            if (!success)
            {
                await Response().Error(strs.event_not_found).SendAsync();
                return;
            }

            await Response()
                .Confirm(strs.event_rsvp(ctx.User.Mention, eventId))
                .SendAsync();
        }

        [Cmd]
        public async Task EventList()
        {
            var events = await _service.GetUpcomingEventsAsync(ctx.Guild.Id);

            if (events.Count == 0)
            {
                await Response().Error(strs.event_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.event_list));

            foreach (var ev in events)
            {
                var rsvpCount = string.IsNullOrWhiteSpace(ev.RsvpUserIds)
                    ? 0
                    : ev.RsvpUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;

                eb.AddField(
                    $"#{ev.Id} — {ev.Title}",
                    $"{ev.Description}\nStarts: {TimestampTag.FromDateTime(ev.StartsAt)} | RSVPs: {rsvpCount}",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        public async Task EventCancel(int eventId)
        {
            var isAdmin = ((IGuildUser)ctx.User).GuildPermissions.ManageGuild;
            var success = await _service.CancelEventAsync(ctx.Guild.Id, eventId, ctx.User.Id, isAdmin);

            if (!success)
            {
                await Response().Error(strs.event_cancel_fail).SendAsync();
                return;
            }

            await Response().Confirm(strs.event_cancelled(eventId)).SendAsync();
        }

        [Cmd]
        public async Task EventPing(int eventId, IRole role)
        {
            var isAdmin = ((IGuildUser)ctx.User).GuildPermissions.ManageGuild;

            if (!isAdmin)
            {
                await Response().Error(strs.event_no_perm).SendAsync();
                return;
            }

            var success = await _service.SetPingRoleAsync(ctx.Guild.Id, eventId, role.Id);

            if (!success)
            {
                await Response().Error(strs.event_not_found).SendAsync();
                return;
            }

            await Response()
                .Confirm(strs.event_ping_set(role.Mention, eventId))
                .SendAsync();
        }
    }
}
