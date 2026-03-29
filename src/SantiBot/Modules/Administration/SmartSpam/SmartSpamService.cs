#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class SmartSpamService : INService, IReadyExecutor, IExecOnMessage
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, SmartSpamConfig> _configs = new();
    private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), List<(DateTime Time, string Content)>> _messageHistory = new();

    public int Priority => 3;

    public SmartSpamService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var configs = await ctx.GetTable<SmartSpamConfig>()
            .Where(x => x.IsEnabled)
            .ToListAsyncLinqToDB();

        foreach (var c in configs)
            _configs[c.GuildId] = c;
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null || msg.Author is not IGuildUser gu || gu.GuildPermissions.ManageMessages)
            return false;

        if (!_configs.TryGetValue(guild.Id, out var config) || !config.IsEnabled)
            return false;

        var key = (guild.Id, msg.Author.Id);
        var now = DateTime.UtcNow;

        var history = _messageHistory.GetOrAdd(key, _ => new List<(DateTime, string)>());

        lock (history)
        {
            history.Add((now, msg.Content));
            // Keep only last 30 seconds of messages
            history.RemoveAll(x => (now - x.Time).TotalSeconds > 30);
        }

        var score = CalculateSpamScore(msg.Content, history);

        if (score < config.Threshold)
            return false;

        try
        {
            switch (config.Action)
            {
                case "delete":
                    await msg.DeleteAsync();
                    break;
                case "warn":
                    await msg.DeleteAsync();
                    break;
                case "mute":
                    await msg.DeleteAsync();
                    if (gu is SocketGuildUser sgu)
                        await sgu.SetTimeOutAsync(TimeSpan.FromMinutes(5));
                    break;
            }
        }
        catch { }

        return true;
    }

    private static int CalculateSpamScore(string content, List<(DateTime Time, string Content)> history)
    {
        var score = 0;

        // Message frequency (messages in last 10 seconds)
        var recentCount = history.Count(x => (DateTime.UtcNow - x.Time).TotalSeconds <= 10);
        score += Math.Min(recentCount * 10, 40);

        // Caps ratio
        if (content.Length > 5)
        {
            var capsRatio = (double)content.Count(char.IsUpper) / content.Length;
            if (capsRatio > 0.7) score += 20;
            else if (capsRatio > 0.5) score += 10;
        }

        // Emoji density
        var emojiCount = content.Count(c => char.IsHighSurrogate(c) || c is ':');
        if (content.Length > 0)
        {
            var emojiRatio = (double)emojiCount / content.Length;
            if (emojiRatio > 0.3) score += 15;
        }

        // Link density
        var linkCount = content.Split("http").Length - 1;
        if (linkCount >= 3) score += 20;
        else if (linkCount >= 2) score += 10;

        // Duplicate text detection
        var duplicates = history.Count(x => x.Content == content);
        if (duplicates >= 3) score += 25;
        else if (duplicates >= 2) score += 15;

        return Math.Min(score, 100);
    }

    public async Task SetConfigAsync(ulong guildId, bool enabled, int? threshold = null, string action = null)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<SmartSpamConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<SmartSpamConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new SmartSpamConfig
                {
                    IsEnabled = enabled,
                    Threshold = threshold ?? existing.Threshold,
                    Action = action ?? existing.Action
                });

            existing.IsEnabled = enabled;
            if (threshold.HasValue) existing.Threshold = threshold.Value;
            if (action is not null) existing.Action = action;
            _configs[guildId] = existing;
        }
        else
        {
            var config = new SmartSpamConfig
            {
                GuildId = guildId,
                IsEnabled = enabled,
                Threshold = threshold ?? 50,
                Action = action ?? "delete"
            };

            await ctx.GetTable<SmartSpamConfig>()
                .InsertAsync(() => new SmartSpamConfig
                {
                    GuildId = guildId,
                    IsEnabled = enabled,
                    Threshold = config.Threshold,
                    Action = config.Action
                });

            _configs[guildId] = config;
        }
    }

    public SmartSpamConfig GetConfig(ulong guildId)
        => _configs.TryGetValue(guildId, out var c) ? c : null;
}
