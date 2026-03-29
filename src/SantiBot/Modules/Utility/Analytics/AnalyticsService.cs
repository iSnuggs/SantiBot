#nullable disable
using SantiBot.Common.ModuleBehaviors;
using SysConcurrent = System.Collections.Concurrent;

namespace SantiBot.Modules.Utility.Analytics;

public sealed class AnalyticsService : INService, IReadyExecutor
{
    private readonly DiscordSocketClient _client;

    // ── Message tracking ──────────────────────────────────────────
    // Key: guildId → list of recorded messages (ring buffer capped to 7 days)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, SysConcurrent.ConcurrentBag<MessageRecord>> _messages = new();

    // ── Growth tracking ───────────────────────────────────────────
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, SysConcurrent.ConcurrentBag<GrowthRecord>> _growth = new();

    // ── Word tracking ─────────────────────────────────────────────
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<string, int>> _words = new();

    // ── Active users (for engagement) ─────────────────────────────
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<ulong, DateTime>> _activeUsers = new();

    private Timer _pruneTimer;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "could",
        "should", "may", "might", "shall", "can", "need", "dare", "ought",
        "used", "to", "of", "in", "for", "on", "with", "at", "by", "from",
        "as", "into", "through", "during", "before", "after", "above", "below",
        "between", "out", "off", "over", "under", "again", "further", "then",
        "once", "here", "there", "when", "where", "why", "how", "all", "each",
        "every", "both", "few", "more", "most", "other", "some", "such", "no",
        "nor", "not", "only", "own", "same", "so", "than", "too", "very",
        "just", "because", "but", "and", "or", "if", "while", "about", "up",
        "that", "this", "it", "its", "i", "me", "my", "we", "our", "you",
        "your", "he", "him", "his", "she", "her", "they", "them", "their",
        "what", "which", "who", "whom", "these", "those", "am", "im", "dont",
        "like", "get", "got", "also", "well", "back", "even", "still", "way",
        "take", "come", "make", "know", "say", "said", "going", "go", "thing",
        "one", "two", "much", "many", "any", "us", "yeah", "yes", "ok", "lol",
        "oh", "really", "right", "see"
    };

    public AnalyticsService(DiscordSocketClient client)
    {
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessageReceived;
        _client.UserJoined += OnUserJoined;
        _client.UserLeft += OnUserLeft;

        // Prune data older than 7 days every hour
        _pruneTimer = new Timer(_ => PruneOldData(), null,
            TimeSpan.FromMinutes(30), TimeSpan.FromHours(1));

        return Task.CompletedTask;
    }

    // ══════════════════════════════════════════════════════════════
    //  Event Handlers
    // ══════════════════════════════════════════════════════════════

    private Task OnMessageReceived(SocketMessage msg)
    {
        if (msg.Author.IsBot || msg.Channel is not ITextChannel textChannel)
            return Task.CompletedTask;

        RecordMessage(textChannel.GuildId, textChannel.Id, msg.Author.Id, DateTime.UtcNow);
        RecordWords(textChannel.GuildId, msg.Content);

        // Track active user
        var activeUsers = _activeUsers.GetOrAdd(textChannel.GuildId, _ => new());
        activeUsers[msg.Author.Id] = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    private Task OnUserJoined(SocketGuildUser user)
    {
        RecordJoin(user.Guild.Id, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    private Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        RecordLeave(guild.Id, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    // ══════════════════════════════════════════════════════════════
    //  1. Message Tracking
    // ══════════════════════════════════════════════════════════════

    public void RecordMessage(ulong guildId, ulong channelId, ulong userId, DateTime timestamp)
    {
        var bag = _messages.GetOrAdd(guildId, _ => new());
        bag.Add(new MessageRecord(channelId, userId, timestamp));
    }

    public List<(DateTime Date, int Count)> GetMessagesPerDay(ulong guildId, int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days).Date;
        var records = GetMessageRecords(guildId, since);

        return Enumerable.Range(0, days)
            .Select(d => since.AddDays(d))
            .Select(date => (date, records.Count(r => r.Timestamp.Date == date)))
            .ToList();
    }

    public List<(ulong ChannelId, int Count)> GetMessagesPerChannel(ulong guildId, int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var records = GetMessageRecords(guildId, since);

        return records
            .GroupBy(r => r.ChannelId)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }

    public int[] GetMessagesPerHour(ulong guildId)
    {
        var hours = new int[24];
        var since = DateTime.UtcNow.AddDays(-7);
        var records = GetMessageRecords(guildId, since);

        foreach (var r in records)
            hours[r.Timestamp.Hour]++;

        return hours;
    }

    public List<(ulong UserId, int Count)> GetTopPosters(ulong guildId, int days = 7, int count = 10)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var records = GetMessageRecords(guildId, since);

        return records
            .GroupBy(r => r.UserId)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .Take(count)
            .ToList();
    }

    // ══════════════════════════════════════════════════════════════
    //  2. Peak Hours Analysis
    // ══════════════════════════════════════════════════════════════

    public List<(int Hour, int Count)> GetPeakHours(ulong guildId)
    {
        var hours = GetMessagesPerHour(guildId);
        return hours
            .Select((count, hour) => (hour, count))
            .OrderByDescending(x => x.count)
            .Take(3)
            .ToList();
    }

    public List<(int Hour, int Count)> GetQuietHours(ulong guildId)
    {
        var hours = GetMessagesPerHour(guildId);
        return hours
            .Select((count, hour) => (hour, count))
            .OrderBy(x => x.count)
            .Take(3)
            .ToList();
    }

    // ══════════════════════════════════════════════════════════════
    //  3. Activity Heatmap (day-of-week × hour-of-day)
    // ══════════════════════════════════════════════════════════════

    public int[,] GetHeatmap(ulong guildId)
    {
        var grid = new int[7, 24]; // [dayOfWeek, hour]
        var since = DateTime.UtcNow.AddDays(-7);
        var records = GetMessageRecords(guildId, since);

        foreach (var r in records)
        {
            var dow = (int)r.Timestamp.DayOfWeek; // Sunday = 0
            grid[dow, r.Timestamp.Hour]++;
        }

        return grid;
    }

    // ══════════════════════════════════════════════════════════════
    //  4. Growth Tracking
    // ══════════════════════════════════════════════════════════════

    public void RecordJoin(ulong guildId, DateTime timestamp)
    {
        var bag = _growth.GetOrAdd(guildId, _ => new());
        bag.Add(new GrowthRecord(timestamp, true));
    }

    public void RecordLeave(ulong guildId, DateTime timestamp)
    {
        var bag = _growth.GetOrAdd(guildId, _ => new());
        bag.Add(new GrowthRecord(timestamp, false));
    }

    public List<(DateTime Date, int Joins, int Leaves, int Net)> GetGrowth(ulong guildId, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days).Date;

        if (!_growth.TryGetValue(guildId, out var bag))
        {
            return Enumerable.Range(0, days)
                .Select(d => (since.AddDays(d), 0, 0, 0))
                .ToList();
        }

        var records = bag.Where(r => r.Timestamp >= since).ToList();

        return Enumerable.Range(0, days)
            .Select(d =>
            {
                var date = since.AddDays(d);
                var dayRecords = records.Where(r => r.Timestamp.Date == date).ToList();
                var joins = dayRecords.Count(r => r.IsJoin);
                var leaves = dayRecords.Count(r => !r.IsJoin);
                return (date, joins, leaves, joins - leaves);
            })
            .ToList();
    }

    // ══════════════════════════════════════════════════════════════
    //  5. Engagement Rate
    // ══════════════════════════════════════════════════════════════

    public double CalculateEngagement(SocketGuild guild)
    {
        if (guild.MemberCount == 0)
            return 0;

        if (!_activeUsers.TryGetValue(guild.Id, out var users))
            return 0;

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var activeCount = users.Count(u => u.Value >= sevenDaysAgo);

        // Exclude bots from total
        var totalMembers = guild.MemberCount;
        if (totalMembers == 0)
            return 0;

        return Math.Round((double)activeCount / totalMembers * 100, 1);
    }

    // ══════════════════════════════════════════════════════════════
    //  6. Channel Rankings
    // ══════════════════════════════════════════════════════════════

    public List<(ulong ChannelId, int Count)> GetChannelRankings(ulong guildId)
        => GetMessagesPerChannel(guildId, 7);

    // ══════════════════════════════════════════════════════════════
    //  7. Word Frequency
    // ══════════════════════════════════════════════════════════════

    public void RecordWords(ulong guildId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var wordCounts = _words.GetOrAdd(guildId, _ => new());

        var tokens = message
            .ToLowerInvariant()
            .Split(new char[] { ' ', '\n', '\r', '\t', ',', '.', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '<', '>' },
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            // Skip URLs, mentions, short words, and stop words
            if (token.Length < 3
                || token.StartsWith("http")
                || token.StartsWith('<')
                || token.StartsWith('@')
                || token.StartsWith('#')
                || StopWords.Contains(token))
                continue;

            wordCounts.AddOrUpdate(token, 1, (_, count) => count + 1);
        }
    }

    public List<(string Word, int Count)> GetTopWords(ulong guildId, int count = 20)
    {
        if (!_words.TryGetValue(guildId, out var wordCounts))
            return new();

        return wordCounts
            .OrderByDescending(x => x.Value)
            .Take(count)
            .Select(x => (x.Key, x.Value))
            .ToList();
    }

    // ══════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════

    private List<MessageRecord> GetMessageRecords(ulong guildId, DateTime since)
    {
        if (!_messages.TryGetValue(guildId, out var bag))
            return new();

        return bag.Where(r => r.Timestamp >= since).ToList();
    }

    private void PruneOldData()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            foreach (var (guildId, bag) in _messages)
            {
                var fresh = bag.Where(r => r.Timestamp >= cutoff).ToList();
                _messages[guildId] = new SysConcurrent.ConcurrentBag<MessageRecord>(fresh);
            }

            foreach (var (guildId, bag) in _growth)
            {
                var cutoff30 = DateTime.UtcNow.AddDays(-30);
                var fresh = bag.Where(r => r.Timestamp >= cutoff30).ToList();
                _growth[guildId] = new SysConcurrent.ConcurrentBag<GrowthRecord>(fresh);
            }

            // Prune active users older than 7 days
            foreach (var (_, users) in _activeUsers)
            {
                var stale = users.Where(u => u.Value < cutoff).Select(u => u.Key).ToList();
                foreach (var uid in stale)
                    users.TryRemove(uid, out _);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error pruning analytics data");
        }
    }

    // ── Internal record types ─────────────────────────────────────

    private readonly record struct MessageRecord(ulong ChannelId, ulong UserId, DateTime Timestamp);
    private readonly record struct GrowthRecord(DateTime Timestamp, bool IsJoin);
}
