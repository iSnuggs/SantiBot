using System.Globalization;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Calendar")]
    [Group("calendar")]
    public partial class CalendarCommands : SantiModule<CalendarIntegration.CalendarService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CalendarAdd([Leftover] string input)
        {
            // Expected: <date> <time> <event title>
            // e.g., "2026-04-15 14:00 Team Meeting"
            var parts = input.Split(' ', 3);
            if (parts.Length < 3)
            {
                await Response().Error("Usage: `.calendar add <date> <time> <event>`\nExample: `.calendar add 2026-04-15 14:00 Team Meeting`").SendAsync();
                return;
            }

            if (!DateTime.TryParse($"{parts[0]} {parts[1]}", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var eventDate))
            {
                await Response().Error("Invalid date/time format. Use: `YYYY-MM-DD HH:mm`").SendAsync();
                return;
            }

            if (eventDate < DateTime.UtcNow)
            {
                await Response().Error("Cannot create events in the past.").SendAsync();
                return;
            }

            var title = parts[2];
            var id = await _service.AddEventAsync(ctx.Guild.Id, ctx.User.Id, eventDate, title);
            var ts = new DateTimeOffset(eventDate).ToUnixTimeSeconds();
            await Response()
                .Confirm($"Event #{id} created: **{title}** on <t:{ts}:F>")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CalendarList(int month = 0)
        {
            int? m = month > 0 && month <= 12 ? month : null;
            var events = await _service.ListEventsAsync(ctx.Guild.Id, m);
            if (events.Count == 0)
            {
                await Response().Error("No upcoming events.").SendAsync();
                return;
            }

            var desc = string.Join("\n", events.Select(e =>
            {
                var ts = new DateTimeOffset(e.EventDate).ToUnixTimeSeconds();
                return $"`#{e.Id}` **{e.Title}** - <t:{ts}:F>";
            }));

            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Upcoming Events").WithDescription(desc))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CalendarToday()
        {
            var events = await _service.TodayEventsAsync(ctx.Guild.Id);
            if (events.Count == 0)
            {
                await Response().Confirm("No events today.").SendAsync();
                return;
            }

            var desc = string.Join("\n", events.Select(e =>
            {
                var ts = new DateTimeOffset(e.EventDate).ToUnixTimeSeconds();
                return $"`#{e.Id}` **{e.Title}** at <t:{ts}:t>";
            }));

            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Today's Events").WithDescription(desc))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CalendarRemove(int id)
        {
            var success = await _service.RemoveEventAsync(ctx.Guild.Id, id);
            if (success)
                await Response().Confirm($"Event #{id} removed.").SendAsync();
            else
                await Response().Error("Event not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CalendarRemind(int id, int minutesBefore)
        {
            var success = await _service.SetReminderAsync(ctx.Guild.Id, id, minutesBefore);
            if (success)
                await Response().Confirm($"Reminder set: {minutesBefore} minutes before event #{id}").SendAsync();
            else
                await Response().Error("Event not found.").SendAsync();
        }
    }
}
