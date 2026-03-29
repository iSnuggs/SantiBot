#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Text;

namespace SantiBot.Modules.Games.Voice;

public sealed class VoiceToolsService : INService
{
    private readonly DbService _db;
    private static readonly SantiRandom _rng = new();

    // Track active temp channels
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, ulong> _tempChannelOwners = new();

    public VoiceToolsService(DbService db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════════════════
    //  SOUNDBOARD
    // ═══════════════════════════════════════════════════════════

    public async Task<SoundboardSound> AddSoundAsync(ulong guildId, string name, string url, ulong addedBy, string category = "General")
    {
        await using var ctx = _db.GetDbContext();
        var sound = new SoundboardSound
        {
            GuildId = guildId, Name = name, Url = url,
            AddedBy = addedBy, Category = category,
        };
        ctx.Add(sound);
        await ctx.SaveChangesAsync();
        return sound;
    }

    public async Task<List<SoundboardSound>> GetSoundsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SoundboardSound>()
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.Category).ThenBy(x => x.Name)
            .ToListAsyncLinqToDB();
    }

    public async Task<SoundboardSound> GetSoundAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SoundboardSound>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId &&
                x.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> RemoveSoundAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<SoundboardSound>()
            .Where(x => x.GuildId == guildId && x.Name.ToLower() == name.ToLower())
            .DeleteAsync();
        return deleted > 0;
    }

    public async Task IncrementPlayCountAsync(int soundId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<SoundboardSound>()
            .Where(x => x.Id == soundId)
            .UpdateAsync(x => new SoundboardSound { PlayCount = x.PlayCount + 1 });
    }

    // ═══════════════════════════════════════════════════════════
    //  TEMP VOICE CHANNELS
    // ═══════════════════════════════════════════════════════════

    public async Task<TempVoiceConfig> GetTempVoiceConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<TempVoiceConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task SetTempVoiceConfigAsync(ulong guildId, ulong createChannelId, ulong categoryId, string defaultName)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<TempVoiceConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<TempVoiceConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new TempVoiceConfig
                {
                    CreateChannelId = createChannelId,
                    CategoryId = categoryId,
                    DefaultName = defaultName,
                    IsEnabled = true,
                });
        }
        else
        {
            ctx.Add(new TempVoiceConfig
            {
                GuildId = guildId,
                CreateChannelId = createChannelId,
                CategoryId = categoryId,
                DefaultName = defaultName,
            });
            await ctx.SaveChangesAsync();
        }
    }

    public async Task TrackTempChannelAsync(ulong guildId, ulong channelId, ulong ownerId, string name)
    {
        _tempChannelOwners[channelId] = ownerId;
        await using var ctx = _db.GetDbContext();
        ctx.Add(new TempVoiceChannel
        {
            GuildId = guildId, ChannelId = channelId,
            OwnerId = ownerId, Name = name,
        });
        await ctx.SaveChangesAsync();
    }

    public bool IsTempChannelOwner(ulong channelId, ulong userId) =>
        _tempChannelOwners.TryGetValue(channelId, out var owner) && owner == userId;

    public void RemoveTempChannel(ulong channelId) =>
        _tempChannelOwners.TryRemove(channelId, out _);

    // ═══════════════════════════════════════════════════════════
    //  VOICE SESSION LOGGING
    // ═══════════════════════════════════════════════════════════

    public async Task LogSessionAsync(ulong userId, ulong guildId, ulong channelId, DateTime joined, DateTime left, bool streaming, bool muted, bool deafened)
    {
        var duration = (int)(left - joined).TotalMinutes;
        if (duration < 1) return;

        await using var ctx = _db.GetDbContext();
        ctx.Add(new VoiceSessionLog
        {
            UserId = userId, GuildId = guildId, ChannelId = channelId,
            JoinedAt = joined, LeftAt = left, DurationMinutes = duration,
            WasStreaming = streaming, WasMuted = muted, WasDeafened = deafened,
        });
        await ctx.SaveChangesAsync();
    }

    public async Task<List<(ulong UserId, int TotalMinutes)>> GetVoiceLeaderboardAsync(ulong guildId, int days = 7)
    {
        await using var ctx = _db.GetDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await ctx.GetTable<VoiceSessionLog>()
            .Where(x => x.GuildId == guildId && x.JoinedAt >= cutoff)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.DurationMinutes) })
            .OrderByDescending(x => x.Total)
            .Take(15)
            .ToListAsyncLinqToDB()
            .ContinueWith(t => t.Result.Select(x => (x.UserId, x.Total)).ToList());
    }

    public async Task<(int TotalMinutes, int Sessions, int StreamMinutes)> GetUserVoiceStatsAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var logs = await ctx.GetTable<VoiceSessionLog>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return (logs.Sum(x => x.DurationMinutes), logs.Count, logs.Where(x => x.WasStreaming).Sum(x => x.DurationMinutes));
    }

    // ═══════════════════════════════════════════════════════════
    //  CHANNEL POINTS
    // ═══════════════════════════════════════════════════════════

    public async Task<ChannelPointsConfig> GetPointsConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<ChannelPointsConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
        if (config is not null) return config;

        config = new ChannelPointsConfig { GuildId = guildId };
        ctx.Add(config);
        await ctx.SaveChangesAsync();
        return config;
    }

    public async Task<long> GetUserPointsAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var user = await ctx.GetTable<UserChannelPoints>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);
        return user?.Points ?? 0;
    }

    public async Task AddPointsAsync(ulong userId, ulong guildId, long amount)
    {
        await using var ctx = _db.GetDbContext();
        var user = await ctx.GetTable<UserChannelPoints>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);

        if (user is null)
        {
            ctx.Add(new UserChannelPoints
            {
                UserId = userId, GuildId = guildId,
                Points = amount, TotalEarned = amount,
            });
            await ctx.SaveChangesAsync();
        }
        else
        {
            await ctx.GetTable<UserChannelPoints>()
                .Where(x => x.Id == user.Id)
                .UpdateAsync(_ => new UserChannelPoints
                {
                    Points = user.Points + amount,
                    TotalEarned = user.TotalEarned + amount,
                });
        }
    }

    public async Task<bool> SpendPointsAsync(ulong userId, ulong guildId, long amount)
    {
        await using var ctx = _db.GetDbContext();
        var user = await ctx.GetTable<UserChannelPoints>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);

        if (user is null || user.Points < amount) return false;

        await ctx.GetTable<UserChannelPoints>()
            .Where(x => x.Id == user.Id)
            .UpdateAsync(_ => new UserChannelPoints
            {
                Points = user.Points - amount,
                TotalSpent = user.TotalSpent + amount,
            });
        return true;
    }

    public async Task<List<ChannelPointReward>> GetRewardsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ChannelPointReward>()
            .Where(x => x.GuildId == guildId && x.IsEnabled)
            .OrderBy(x => x.Cost)
            .ToListAsyncLinqToDB();
    }

    // ═══════════════════════════════════════════════════════════
    //  PREDICTIONS
    // ═══════════════════════════════════════════════════════════

    public async Task<Prediction> CreatePredictionAsync(ulong guildId, ulong channelId, ulong createdBy, string question, string opt1, string opt2)
    {
        await using var ctx = _db.GetDbContext();
        var pred = new Prediction
        {
            GuildId = guildId, ChannelId = channelId, CreatedBy = createdBy,
            Question = question, Option1 = opt1, Option2 = opt2,
        };
        ctx.Add(pred);
        await ctx.SaveChangesAsync();
        return pred;
    }

    public async Task<Prediction> GetActivePredictionAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Prediction>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Status == "Open");
    }

    public async Task<(bool Success, string Message)> PlaceBetAsync(ulong guildId, ulong userId, int option, long points)
    {
        await using var ctx = _db.GetDbContext();
        var pred = await ctx.GetTable<Prediction>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Status == "Open");

        if (pred is null) return (false, "No active prediction!");
        if (option is not (1 or 2)) return (false, "Choose option 1 or 2!");

        var existing = await ctx.GetTable<PredictionBet>()
            .FirstOrDefaultAsyncLinqToDB(x => x.PredictionId == pred.Id && x.UserId == userId);
        if (existing is not null) return (false, "You already bet on this prediction!");

        if (!await SpendPointsAsync(userId, guildId, points))
            return (false, "Not enough channel points!");

        ctx.Add(new PredictionBet { PredictionId = pred.Id, UserId = userId, ChosenOption = option, PointsBet = points });
        await ctx.SaveChangesAsync();

        // Update totals
        if (option == 1)
            await ctx.GetTable<Prediction>().Where(x => x.Id == pred.Id)
                .UpdateAsync(x => new Prediction { Option1Points = x.Option1Points + points, Option1Voters = x.Option1Voters + 1 });
        else
            await ctx.GetTable<Prediction>().Where(x => x.Id == pred.Id)
                .UpdateAsync(x => new Prediction { Option2Points = x.Option2Points + points, Option2Voters = x.Option2Voters + 1 });

        return (true, $"Bet **{points}** points on option {option}!");
    }

    public async Task<(bool Success, string Message)> ResolvePredictionAsync(ulong guildId, int winningOption)
    {
        await using var ctx = _db.GetDbContext();
        var pred = await ctx.GetTable<Prediction>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && (x.Status == "Open" || x.Status == "Locked"));

        if (pred is null) return (false, "No active prediction!");

        await ctx.GetTable<Prediction>().Where(x => x.Id == pred.Id)
            .UpdateAsync(_ => new Prediction { Status = "Resolved", WinningOption = winningOption });

        var totalPool = pred.Option1Points + pred.Option2Points;
        var winners = await ctx.GetTable<PredictionBet>()
            .Where(x => x.PredictionId == pred.Id && x.ChosenOption == winningOption)
            .ToListAsyncLinqToDB();

        var winningPool = winningOption == 1 ? pred.Option1Points : pred.Option2Points;
        var sb = new StringBuilder();
        sb.AppendLine($"**Option {winningOption} wins!** Total pool: {totalPool} points\n");

        foreach (var bet in winners)
        {
            var payout = winningPool > 0 ? (long)((double)bet.PointsBet / winningPool * totalPool) : bet.PointsBet;
            await AddPointsAsync(bet.UserId, guildId, payout);
            sb.AppendLine($"<@{bet.UserId}> wins **{payout}** points!");
        }

        if (winners.Count == 0)
            sb.AppendLine("No one bet on the winning option!");

        return (true, sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  FAN ART
    // ═══════════════════════════════════════════════════════════

    public async Task<FanArtSubmission> SubmitFanArtAsync(ulong guildId, ulong userId, string title, string imageUrl)
    {
        await using var ctx = _db.GetDbContext();
        var submission = new FanArtSubmission
        {
            GuildId = guildId, UserId = userId, Title = title, ImageUrl = imageUrl,
        };
        ctx.Add(submission);
        await ctx.SaveChangesAsync();
        return submission;
    }

    public async Task<List<FanArtSubmission>> GetFanArtAsync(ulong guildId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<FanArtSubmission>()
            .Where(x => x.GuildId == guildId && x.IsApproved)
            .OrderByDescending(x => x.Votes)
            .Take(count)
            .ToListAsyncLinqToDB();
    }

    public async Task VoteFanArtAsync(int submissionId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<FanArtSubmission>()
            .Where(x => x.Id == submissionId)
            .UpdateAsync(x => new FanArtSubmission { Votes = x.Votes + 1 });
    }

    // ═══════════════════════════════════════════════════════════
    //  FEED SUBSCRIPTIONS
    // ═══════════════════════════════════════════════════════════

    public async Task<FeedSubscription> AddFeedAsync(ulong guildId, ulong channelId, string feedType, string feedUrl, string feedName, ulong addedBy)
    {
        await using var ctx = _db.GetDbContext();
        var feed = new FeedSubscription
        {
            GuildId = guildId, ChannelId = channelId, FeedType = feedType,
            FeedUrl = feedUrl, FeedName = feedName, AddedBy = addedBy,
        };
        ctx.Add(feed);
        await ctx.SaveChangesAsync();
        return feed;
    }

    public async Task<List<FeedSubscription>> GetFeedsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<FeedSubscription>()
            .Where(x => x.GuildId == guildId && x.IsEnabled)
            .OrderBy(x => x.FeedType)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> RemoveFeedAsync(ulong guildId, int feedId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<FeedSubscription>()
            .Where(x => x.GuildId == guildId && x.Id == feedId)
            .DeleteAsync();
        return deleted > 0;
    }

    // ═══════════════════════════════════════════════════════════
    //  UPTIME MONITORING
    // ═══════════════════════════════════════════════════════════

    public async Task<UptimeMonitor> AddMonitorAsync(ulong guildId, ulong alertChannelId, string url, string name)
    {
        await using var ctx = _db.GetDbContext();
        var monitor = new UptimeMonitor
        {
            GuildId = guildId, AlertChannelId = alertChannelId,
            Url = url, Name = name,
        };
        ctx.Add(monitor);
        await ctx.SaveChangesAsync();
        return monitor;
    }

    public async Task<List<UptimeMonitor>> GetMonitorsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UptimeMonitor>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> RemoveMonitorAsync(ulong guildId, int monitorId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<UptimeMonitor>()
            .Where(x => x.GuildId == guildId && x.Id == monitorId)
            .DeleteAsync();
        return deleted > 0;
    }
}
