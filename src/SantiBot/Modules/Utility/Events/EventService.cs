using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.Events;

public sealed class EventService : INService
{
    private readonly DbService _db;

    public EventService(DbService db)
    {
        _db = db;
    }

    // ── Create / Query Events ──────────────────────────────────────

    public async Task<ServerEvent> CreateEvent(
        ulong guildId,
        ulong channelId,
        ulong createdBy,
        string title,
        string description,
        string eventType,
        DateTime startTime,
        DateTime? endTime,
        int maxAttendees = 0)
    {
        await using var ctx = _db.GetDbContext();

        var ev = await ctx.GetTable<ServerEvent>()
            .InsertWithOutputAsync(() => new ServerEvent
            {
                GuildId = guildId,
                ChannelId = channelId,
                CreatedBy = createdBy,
                Title = title,
                Description = description,
                EventType = eventType,
                StartTime = startTime,
                EndTime = endTime,
                MaxAttendees = maxAttendees,
                Status = "Upcoming",
            });

        // Create a default 15-minute reminder
        await ctx.GetTable<EventReminder>()
            .InsertAsync(() => new EventReminder
            {
                EventId = ev.Id,
                GuildId = guildId,
                ReminderMinutesBefore = 15,
                Sent = false,
            });

        return ev;
    }

    public async Task<IReadOnlyList<ServerEvent>> GetUpcomingEvents(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<ServerEvent>()
            .Where(x => x.GuildId == guildId
                         && (x.Status == "Upcoming" || x.Status == "Active")
                         && x.StartTime > DateTime.UtcNow)
            .OrderBy(x => x.StartTime)
            .Take(25)
            .ToListAsyncLinqToDB();
    }

    public async Task<ServerEvent?> GetEvent(int eventId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<ServerEvent>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == eventId);
    }

    // ── RSVP ───────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> RsvpEvent(
        int eventId, ulong userId, ulong guildId, string status)
    {
        await using var ctx = _db.GetDbContext();

        var ev = await ctx.GetTable<ServerEvent>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == eventId && x.GuildId == guildId);

        if (ev is null)
            return (false, "Event not found.");

        if (ev.Status == "Cancelled")
            return (false, "That event has been cancelled.");

        if (ev.Status == "Completed")
            return (false, "That event has already ended.");

        // Check max attendees (0 = unlimited)
        if (ev.MaxAttendees > 0 && status == "Going")
        {
            var goingCount = await ctx.GetTable<EventRsvp>()
                .CountAsyncLinqToDB(x => x.EventId == eventId && x.Status == "Going");

            if (goingCount >= ev.MaxAttendees)
                return (false, "This event is full.");
        }

        // Upsert: delete existing RSVP then insert new one
        await ctx.GetTable<EventRsvp>()
            .Where(x => x.EventId == eventId && x.UserId == userId)
            .DeleteAsync();

        await ctx.GetTable<EventRsvp>()
            .InsertAsync(() => new EventRsvp
            {
                EventId = eventId,
                UserId = userId,
                GuildId = guildId,
                Status = status,
                RsvpAt = DateTime.UtcNow,
            });

        return (true, $"You are now marked as **{status}** for **{ev.Title}**.");
    }

    public async Task<IReadOnlyList<EventRsvp>> GetRsvps(int eventId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<EventRsvp>()
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.RsvpAt)
            .ToListAsyncLinqToDB();
    }

    // ── Cancel ─────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> CancelEvent(
        int eventId, ulong userId, bool isAdmin)
    {
        await using var ctx = _db.GetDbContext();

        var ev = await ctx.GetTable<ServerEvent>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == eventId);

        if (ev is null)
            return (false, "Event not found.");

        if (ev.CreatedBy != userId && !isAdmin)
            return (false, "Only the event creator or a server admin can cancel events.");

        await ctx.GetTable<ServerEvent>()
            .Where(x => x.Id == eventId)
            .Set(x => x.Status, "Cancelled")
            .UpdateAsync();

        return (true, $"Event **{ev.Title}** (#{ev.Id}) has been cancelled.");
    }

    // ── Movie Night Poll ───────────────────────────────────────────

    public async Task<MovieNightPoll> StartMovieNightPoll(
        ulong guildId, ulong channelId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        // Close any existing active poll for this guild
        await ctx.GetTable<MovieNightPoll>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .Set(x => x.IsActive, false)
            .UpdateAsync();

        var poll = await ctx.GetTable<MovieNightPoll>()
            .InsertWithOutputAsync(() => new MovieNightPoll
            {
                GuildId = guildId,
                ChannelId = channelId,
                IsActive = true,
                CreatedBy = userId,
            });

        return poll;
    }

    public async Task<(bool Success, string Message)> AddMovieOption(
        int pollId, string title, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var poll = await ctx.GetTable<MovieNightPoll>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == pollId && x.IsActive);

        if (poll is null)
            return (false, "No active movie poll found.");

        // Prevent duplicates
        var exists = await ctx.GetTable<MovieNightOption>()
            .AnyAsyncLinqToDB(x => x.PollId == pollId
                                    && x.MovieTitle.ToLower() == title.ToLower());

        if (exists)
            return (false, "That movie has already been added.");

        await ctx.GetTable<MovieNightOption>()
            .InsertAsync(() => new MovieNightOption
            {
                PollId = pollId,
                MovieTitle = title,
                AddedBy = userId,
                Votes = 0,
            });

        return (true, $"**{title}** has been added to the movie poll!");
    }

    public async Task<(bool Success, string Message)> VoteMovie(
        int pollId, string movieTitle, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var poll = await ctx.GetTable<MovieNightPoll>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == pollId && x.IsActive);

        if (poll is null)
            return (false, "No active movie poll found.");

        var option = await ctx.GetTable<MovieNightOption>()
            .FirstOrDefaultAsyncLinqToDB(x => x.PollId == pollId
                                               && x.MovieTitle.ToLower() == movieTitle.ToLower());

        if (option is null)
            return (false, "That movie is not in the poll. Use `.movie add` first.");

        await ctx.GetTable<MovieNightOption>()
            .Where(x => x.Id == option.Id)
            .Set(x => x.Votes, x => x.Votes + 1)
            .UpdateAsync();

        return (true, $"Your vote for **{option.MovieTitle}** has been counted!");
    }

    public async Task<IReadOnlyList<MovieNightOption>> GetMoviePollResults(int pollId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<MovieNightOption>()
            .Where(x => x.PollId == pollId)
            .OrderByDescending(x => x.Votes)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool Success, MovieNightOption? Winner, string Message)> EndMoviePoll(
        int pollId)
    {
        await using var ctx = _db.GetDbContext();

        var poll = await ctx.GetTable<MovieNightPoll>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == pollId && x.IsActive);

        if (poll is null)
            return (false, null, "No active movie poll found.");

        // Close the poll
        await ctx.GetTable<MovieNightPoll>()
            .Where(x => x.Id == pollId)
            .Set(x => x.IsActive, false)
            .UpdateAsync();

        var winner = await ctx.GetTable<MovieNightOption>()
            .Where(x => x.PollId == pollId)
            .OrderByDescending(x => x.Votes)
            .FirstOrDefaultAsyncLinqToDB();

        if (winner is null)
            return (true, null, "The poll has ended but no movies were added.");

        return (true, winner, $"The winner is **{winner.MovieTitle}** with **{winner.Votes}** vote(s)!");
    }

    /// <summary>
    /// Get the currently active movie poll for this guild, if any.
    /// </summary>
    public async Task<MovieNightPoll?> GetActiveMoviePoll(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<MovieNightPoll>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.IsActive);
    }

    // ── Teams ──────────────────────────────────────────────────────

    public static List<List<T>> RandomTeams<T>(IReadOnlyList<T> users, int teamCount)
    {
        var rng = new Random();
        var shuffled = users.OrderBy(_ => rng.Next()).ToList();
        var teams = new List<List<T>>();

        for (var i = 0; i < teamCount; i++)
            teams.Add(new List<T>());

        for (var i = 0; i < shuffled.Count; i++)
            teams[i % teamCount].Add(shuffled[i]);

        return teams;
    }

    public static List<List<T>> BalancedTeams<T>(IReadOnlyList<T> users, int teamCount)
    {
        // Balanced by count — same as random but without shuffling
        // so voice channel order is preserved, giving a "fair" split
        var teams = new List<List<T>>();

        for (var i = 0; i < teamCount; i++)
            teams.Add(new List<T>());

        for (var i = 0; i < users.Count; i++)
            teams[i % teamCount].Add(users[i]);

        return teams;
    }
}
