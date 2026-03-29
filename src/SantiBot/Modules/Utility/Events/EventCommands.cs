using System.Globalization;
using SantiBot.Modules.Utility.Events;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Events")]
    [Group("event")]
    public partial class EventCommands : SantiModule<EventService>
    {
        // ── Event Create ───────────────────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EventCreate(string title, string date, [Leftover] string description = "")
        {
            if (!DateTime.TryParse(date, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var startTime))
            {
                await Response().Error("Invalid date format. Use something like `2026-04-05 20:00`").SendAsync();
                return;
            }

            if (startTime <= DateTime.UtcNow)
            {
                await Response().Error("You can't create an event in the past!").SendAsync();
                return;
            }

            var ev = await _service.CreateEvent(
                ctx.Guild.Id,
                ctx.Channel.Id,
                ctx.User.Id,
                title,
                description,
                "Custom",
                startTime,
                null);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Event Created!")
                .WithDescription(ev.Title)
                .AddField("Description", string.IsNullOrWhiteSpace(ev.Description) ? "No description" : ev.Description, false)
                .AddField("Starts At", TimestampTag.FromDateTime(ev.StartTime).ToString(), true)
                .AddField("Type", ev.EventType, true)
                .WithFooter($"Event #{ev.Id} -- Use .event rsvp {ev.Id} going to join!");

            await Response().Embed(eb).SendAsync();
        }

        // ── Event List ─────────────────────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EventList()
        {
            var events = await _service.GetUpcomingEvents(ctx.Guild.Id);

            if (events.Count == 0)
            {
                await Response().Error("No upcoming events. Create one with `.event create`!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Upcoming Events");

            foreach (var ev in events)
            {
                eb.AddField(
                    $"#{ev.Id} -- {ev.Title} [{ev.EventType}]",
                    $"{ev.Description}\nStarts: {TimestampTag.FromDateTime(ev.StartTime)} | Status: {ev.Status}",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        // ── Event Info ─────────────────────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EventInfo(int id)
        {
            var ev = await _service.GetEvent(id);

            if (ev is null || ev.GuildId != ctx.Guild.Id)
            {
                await Response().Error("Event not found.").SendAsync();
                return;
            }

            var rsvps = await _service.GetRsvps(id);
            var going = rsvps.Where(r => r.Status == "Going").ToList();
            var maybe = rsvps.Where(r => r.Status == "Maybe").ToList();
            var notGoing = rsvps.Where(r => r.Status == "NotGoing").ToList();

            var goingText = going.Count > 0
                ? string.Join(", ", going.Select(r => $"<@{r.UserId}>"))
                : "None";

            var maybeText = maybe.Count > 0
                ? string.Join(", ", maybe.Select(r => $"<@{r.UserId}>"))
                : "None";

            var notGoingText = notGoing.Count > 0
                ? string.Join(", ", notGoing.Select(r => $"<@{r.UserId}>"))
                : "None";

            var capacityText = ev.MaxAttendees > 0
                ? $"{going.Count}/{ev.MaxAttendees}"
                : $"{going.Count} (unlimited)";

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Event #{ev.Id}: {ev.Title}")
                .WithDescription(string.IsNullOrWhiteSpace(ev.Description) ? "No description" : ev.Description)
                .AddField("Type", ev.EventType, true)
                .AddField("Status", ev.Status, true)
                .AddField("Capacity", capacityText, true)
                .AddField("Starts At", TimestampTag.FromDateTime(ev.StartTime).ToString(), true)
                .AddField("End Time", ev.EndTime.HasValue ? TimestampTag.FromDateTime(ev.EndTime.Value).ToString() : "Open-ended", true)
                .AddField("Created By", $"<@{ev.CreatedBy}>", true)
                .AddField($"Going ({going.Count})", goingText, false)
                .AddField($"Maybe ({maybe.Count})", maybeText, false)
                .AddField($"Not Going ({notGoing.Count})", notGoingText, false)
                .WithFooter($"RSVP with .event rsvp {ev.Id} going/maybe/notgoing");

            await Response().Embed(eb).SendAsync();
        }

        // ── Event RSVP ─────────────────────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EventRsvp(int id, [Leftover] string status = "going")
        {
            var normalizedStatus = status.ToLower().Trim() switch
            {
                "going" or "go" or "yes" => "Going",
                "maybe" or "idk" => "Maybe",
                "notgoing" or "no" or "not going" or "decline" => "NotGoing",
                _ => ""
            };

            if (string.IsNullOrEmpty(normalizedStatus))
            {
                await Response().Error("Invalid RSVP status. Use `going`, `maybe`, or `notgoing`.").SendAsync();
                return;
            }

            var (success, message) = await _service.RsvpEvent(id, ctx.User.Id, ctx.Guild.Id, normalizedStatus);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            await Response().Confirm(message).SendAsync();
        }

        // ── Event Cancel ───────────────────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EventCancel(int id)
        {
            var isAdmin = ((IGuildUser)ctx.User).GuildPermissions.ManageGuild;
            var (success, message) = await _service.CancelEvent(id, ctx.User.Id, isAdmin);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            await Response().Confirm(message).SendAsync();
        }

        // ── Movie Night ────────────────────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MovieNight()
        {
            var poll = await _service.StartMovieNightPoll(
                ctx.Guild.Id, ctx.Channel.Id, ctx.User.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Movie Night Poll Started!")
                .WithDescription(
                    "A new movie night poll is now open!\n\n" +
                    $"**Add movies:** `.movie add <title>`\n" +
                    $"**Vote:** `.movie vote <title>`\n" +
                    $"**See results:** `.movie results`\n" +
                    $"**Pick winner:** `.movie pick`")
                .WithFooter($"Poll #{poll.Id} -- started by {ctx.User.Username}");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MovieAdd([Leftover] string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await Response().Error("Please provide a movie title.").SendAsync();
                return;
            }

            var poll = await _service.GetActiveMoviePoll(ctx.Guild.Id);

            if (poll is null)
            {
                await Response().Error("No active movie poll. Start one with `.movie night`!").SendAsync();
                return;
            }

            var (success, message) = await _service.AddMovieOption(poll.Id, title.Trim(), ctx.User.Id);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            await Response().Confirm(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MovieVote([Leftover] string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await Response().Error("Please provide the movie title you want to vote for.").SendAsync();
                return;
            }

            var poll = await _service.GetActiveMoviePoll(ctx.Guild.Id);

            if (poll is null)
            {
                await Response().Error("No active movie poll.").SendAsync();
                return;
            }

            var (success, message) = await _service.VoteMovie(poll.Id, title.Trim(), ctx.User.Id);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            await Response().Confirm(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MovieResults()
        {
            var poll = await _service.GetActiveMoviePoll(ctx.Guild.Id);

            if (poll is null)
            {
                await Response().Error("No active movie poll.").SendAsync();
                return;
            }

            var results = await _service.GetMoviePollResults(poll.Id);

            if (results.Count == 0)
            {
                await Response().Error("No movies have been added yet. Use `.movie add <title>`!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Movie Night Poll Results");

            var desc = "";
            var rank = 1;
            foreach (var option in results)
            {
                var bar = new string('|', option.Votes);
                desc += $"**{rank}.** {option.MovieTitle} -- {option.Votes} vote(s) {bar}\n";
                rank++;
            }

            eb.WithDescription(desc);
            eb.WithFooter($"Poll #{poll.Id} -- vote with .movie vote <title>");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MoviePick()
        {
            var poll = await _service.GetActiveMoviePoll(ctx.Guild.Id);

            if (poll is null)
            {
                await Response().Error("No active movie poll to close.").SendAsync();
                return;
            }

            // Only creator or admin can end the poll
            var isAdmin = ((IGuildUser)ctx.User).GuildPermissions.ManageGuild;
            if (poll.CreatedBy != ctx.User.Id && !isAdmin)
            {
                await Response().Error("Only the poll creator or a server admin can end the poll.").SendAsync();
                return;
            }

            var (success, winner, message) = await _service.EndMoviePoll(poll.Id);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            if (winner is null)
            {
                await Response().Confirm(message).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Movie Night Winner!")
                .WithDescription(
                    $"The votes are in!\n\n" +
                    $"**{winner.MovieTitle}** wins with **{winner.Votes}** vote(s)!\n\n" +
                    $"Enjoy the movie, everyone!")
                .WithFooter($"Poll #{poll.Id} is now closed");

            await Response().Embed(eb).SendAsync();
        }

        // ── Game Night (quick create) ──────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GameNight([Leftover] string dateAndDesc = "")
        {
            // Default to 2 hours from now if no date provided
            var startTime = DateTime.UtcNow.AddHours(2);
            var description = "Game Night! Jump in voice and let's play!";

            if (!string.IsNullOrWhiteSpace(dateAndDesc))
            {
                // Try to parse a date from the start of the string
                var parts = dateAndDesc.Split(' ', 2);
                if (DateTime.TryParse(parts[0], CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var parsed) && parsed > DateTime.UtcNow)
                {
                    startTime = parsed;
                    if (parts.Length > 1)
                        description = parts[1];
                }
                else
                {
                    // Entire string is description, use default time
                    description = dateAndDesc;
                }
            }

            var ev = await _service.CreateEvent(
                ctx.Guild.Id,
                ctx.Channel.Id,
                ctx.User.Id,
                "Game Night",
                description,
                "GameNight",
                startTime,
                null);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Game Night Created!")
                .WithDescription(description)
                .AddField("Starts At", TimestampTag.FromDateTime(ev.StartTime).ToString(), true)
                .AddField("RSVP", $"Use `.event rsvp {ev.Id} going` to join!", true)
                .WithFooter($"Event #{ev.Id}");

            await Response().Embed(eb).SendAsync();
        }

        // ── Teams (split voice channel users) ──────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Teams(int count = 2)
        {
            if (count < 2 || count > 20)
            {
                await Response().Error("Team count must be between 2 and 20.").SendAsync();
                return;
            }

            var guildUser = (IGuildUser)ctx.User;
            var voiceChannel = guildUser.VoiceChannel;

            if (voiceChannel is null)
            {
                await Response().Error("You need to be in a voice channel to split into teams.").SendAsync();
                return;
            }

            var users = (await voiceChannel.GetUsersAsync().FlattenAsync())
                .Where(u => !u.IsBot)
                .ToList();

            if (users.Count < count)
            {
                await Response().Error($"Not enough people in voice ({users.Count}) to make {count} teams.").SendAsync();
                return;
            }

            var teams = EventService.RandomTeams<IGuildUser>(users, count);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Random Teams (from {voiceChannel.Name})");

            for (var i = 0; i < teams.Count; i++)
            {
                var members = string.Join("\n", teams[i].Select(u => u.Mention));
                eb.AddField($"Team {i + 1} ({teams[i].Count} players)", members, true);
            }

            eb.WithFooter("Teams were randomly assigned. Good luck!");

            await Response().Embed(eb).SendAsync();
        }

        // ── Study Session ──────────────────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task StudySession(int durationMinutes = 60)
        {
            if (durationMinutes < 10 || durationMinutes > 480)
            {
                await Response().Error("Duration must be between 10 and 480 minutes.").SendAsync();
                return;
            }

            var startTime = DateTime.UtcNow.AddMinutes(5); // starts in 5 min
            var endTime = startTime.AddMinutes(durationMinutes);

            // Calculate pomodoro cycles (25 min work + 5 min break)
            var cycles = durationMinutes / 30;
            var remaining = durationMinutes % 30;

            var description =
                $"Study session for **{durationMinutes} minutes**\n\n" +
                $"**Pomodoro Plan:**\n";

            if (cycles > 0)
            {
                description += $"- {cycles} pomodoro cycle(s) (25 min focus + 5 min break each)\n";
            }

            if (remaining > 0)
            {
                description += $"- {remaining} min bonus focus time at the end\n";
            }

            description += $"\n**Tips:**\n" +
                           $"- Mute notifications during focus blocks\n" +
                           $"- Stay in voice for accountability\n" +
                           $"- Share what you're working on!";

            var ev = await _service.CreateEvent(
                ctx.Guild.Id,
                ctx.Channel.Id,
                ctx.User.Id,
                "Study Session",
                description,
                "StudySession",
                startTime,
                endTime);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Study Session Created!")
                .WithDescription(description)
                .AddField("Starts At", TimestampTag.FromDateTime(ev.StartTime).ToString(), true)
                .AddField("Ends At", TimestampTag.FromDateTime(endTime).ToString(), true)
                .AddField("Duration", $"{durationMinutes} minutes", true)
                .AddField("RSVP", $"Use `.event rsvp {ev.Id} going` to join!", false)
                .WithFooter($"Event #{ev.Id}");

            await Response().Embed(eb).SendAsync();
        }
    }
}
