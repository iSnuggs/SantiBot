#nullable disable
using System.Text.Json;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.Migration;

public sealed class MigrationService : INService
{
    private readonly DbService _db;

    public MigrationService(DbService db)
    {
        _db = db;
    }

    // XP formula matching the rest of SantiBot: level * level * 100
    public static long XpForLevel(int level) => (long)level * level * 100;

    public static int LevelFromXp(long xp)
    {
        if (xp <= 0) return 0;
        return (int)Math.Floor(Math.Sqrt(xp / 100.0));
    }

    // ── MEE6 Import ──────────────────────────────────────────────
    // Format: {"players": [{"id": "123", "username": "user", "xp": 5000, "level": 10, "message_count": 500}]}
    public async Task<(int Imported, int Skipped, int Errors)> ImportMee6Levels(
        ulong guildId, string jsonData, bool merge = true)
    {
        int imported = 0, skipped = 0, errors = 0;

        Mee6Export export;
        try
        {
            export = JsonSerializer.Deserialize<Mee6Export>(jsonData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return (0, 0, 1);
        }

        if (export?.Players is null || export.Players.Count == 0)
            return (0, 0, 0);

        await using var ctx = _db.GetDbContext();

        foreach (var player in export.Players)
        {
            if (!ulong.TryParse(player.Id, out var userId) || userId == 0)
            {
                errors++;
                continue;
            }

            long xp = player.Xp > 0 ? player.Xp : XpForLevel(player.Level);

            try
            {
                await UpsertXp(ctx, guildId, userId, xp, merge);
                imported++;
            }
            catch
            {
                errors++;
            }
        }

        return (imported, skipped, errors);
    }

    // ── Dyno Import ──────────────────────────────────────────────
    // Format: [{"user": "123", "level": 15, "xp": 8000}]
    public async Task<(int Imported, int Skipped, int Errors)> ImportDynoLevels(
        ulong guildId, string jsonData, bool merge = true)
    {
        int imported = 0, skipped = 0, errors = 0;

        List<DynoEntry> entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<DynoEntry>>(jsonData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return (0, 0, 1);
        }

        if (entries is null || entries.Count == 0)
            return (0, 0, 0);

        await using var ctx = _db.GetDbContext();

        foreach (var entry in entries)
        {
            if (!ulong.TryParse(entry.User, out var userId) || userId == 0)
            {
                errors++;
                continue;
            }

            long xp = entry.Xp > 0 ? entry.Xp : XpForLevel(entry.Level);

            try
            {
                await UpsertXp(ctx, guildId, userId, xp, merge);
                imported++;
            }
            catch
            {
                errors++;
            }
        }

        return (imported, skipped, errors);
    }

    // ── Carl-bot Import ──────────────────────────────────────────
    // Format CSV: user_id,level,xp\n123,10,5000\n456,15,8000
    public async Task<(int Imported, int Skipped, int Errors)> ImportCarlLevels(
        ulong guildId, string csvData, bool merge = true)
    {
        int imported = 0, skipped = 0, errors = 0;

        var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
            return (0, 0, 0);

        // Skip header row
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                errors++;
                continue;
            }

            if (!ulong.TryParse(parts[0], out var userId) || userId == 0)
            {
                errors++;
                continue;
            }

            if (!int.TryParse(parts[1], out var level))
                level = 0;

            if (!long.TryParse(parts[2], out var xp))
                xp = 0;

            if (xp <= 0 && level > 0)
                xp = XpForLevel(level);

            if (xp <= 0)
            {
                skipped++;
                continue;
            }

            try
            {
                await using var ctx = _db.GetDbContext();
                await UpsertXp(ctx, guildId, userId, xp, merge);
                imported++;
            }
            catch
            {
                errors++;
            }
        }

        return (imported, skipped, errors);
    }

    // ── Generic CSV Import ───────────────────────────────────────
    public async Task<(int Imported, int Skipped, int Errors)> ImportGenericCsv(
        ulong guildId, string csvData, string userIdCol, string xpCol, string levelCol, bool merge = true)
    {
        int imported = 0, skipped = 0, errors = 0;

        var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
            return (0, 0, 0);

        // Parse header to find column indices
        var headers = lines[0].Split(',', StringSplitOptions.TrimEntries);
        int userIdIdx = Array.FindIndex(headers, h => h.Equals(userIdCol, StringComparison.OrdinalIgnoreCase));
        int xpIdx = string.IsNullOrEmpty(xpCol)
            ? -1
            : Array.FindIndex(headers, h => h.Equals(xpCol, StringComparison.OrdinalIgnoreCase));
        int levelIdx = string.IsNullOrEmpty(levelCol)
            ? -1
            : Array.FindIndex(headers, h => h.Equals(levelCol, StringComparison.OrdinalIgnoreCase));

        if (userIdIdx == -1)
            return (0, 0, 1); // Can't find user ID column

        if (xpIdx == -1 && levelIdx == -1)
            return (0, 0, 1); // Need at least XP or level column

        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length <= userIdIdx)
            {
                errors++;
                continue;
            }

            if (!ulong.TryParse(parts[userIdIdx], out var userId) || userId == 0)
            {
                errors++;
                continue;
            }

            long xp = 0;
            int level = 0;

            if (xpIdx >= 0 && xpIdx < parts.Length)
                long.TryParse(parts[xpIdx], out xp);

            if (levelIdx >= 0 && levelIdx < parts.Length)
                int.TryParse(parts[levelIdx], out level);

            if (xp <= 0 && level > 0)
                xp = XpForLevel(level);

            if (xp <= 0)
            {
                skipped++;
                continue;
            }

            try
            {
                await using var ctx = _db.GetDbContext();
                await UpsertXp(ctx, guildId, userId, xp, merge);
                imported++;
            }
            catch
            {
                errors++;
            }
        }

        return (imported, skipped, errors);
    }

    // ── Export ────────────────────────────────────────────────────
    public async Task<string> ExportLevels(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var stats = await ctx.GetTable<UserXpStats>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Xp)
            .ToListAsyncLinqToDB();

        var export = stats.Select(s => new ExportEntry
        {
            UserId = s.UserId.ToString(),
            Xp = s.Xp,
            Level = LevelFromXp(s.Xp)
        }).ToList();

        return JsonSerializer.Serialize(new { players = export },
            new JsonSerializerOptions { WriteIndented = true });
    }

    // ── Preview ──────────────────────────────────────────────────
    public MigrationPreviewResult MigrationPreview(ulong guildId, string data, string format)
    {
        var result = new MigrationPreviewResult();

        try
        {
            var entries = format.ToLowerInvariant() switch
            {
                "mee6" => ParseMee6Preview(data),
                "dyno" => ParseDynoPreview(data),
                "carl" => ParseCarlPreview(data),
                _ => ParseCarlPreview(data) // default to CSV
            };

            result.UserCount = entries.Count;
            if (entries.Count > 0)
            {
                result.TotalXp = entries.Sum(e => e.Xp);
                result.HighestLevel = entries.Max(e => e.Level);
                result.LowestLevel = entries.Min(e => e.Level);
                result.AverageLevel = (int)entries.Average(e => e.Level);
            }
        }
        catch
        {
            result.ParseError = true;
        }

        return result;
    }

    // ── Private helpers ──────────────────────────────────────────

    private static async Task UpsertXp(SantiContext ctx, ulong guildId, ulong userId, long xp, bool merge)
    {
        var existing = await ctx.GetTable<UserXpStats>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is not null)
        {
            long newXp = merge ? existing.Xp + xp : xp;
            await ctx.GetTable<UserXpStats>()
                .Where(x => x.GuildId == guildId && x.UserId == userId)
                .UpdateAsync(x => new UserXpStats
                {
                    Xp = newXp
                });
        }
        else
        {
            await ctx.GetTable<UserXpStats>()
                .InsertAsync(() => new UserXpStats
                {
                    GuildId = guildId,
                    UserId = userId,
                    Xp = xp
                });
        }
    }

    private static List<PreviewEntry> ParseMee6Preview(string jsonData)
    {
        var export = JsonSerializer.Deserialize<Mee6Export>(jsonData,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (export?.Players is null)
            return new List<PreviewEntry>();

        return export.Players
            .Where(p => ulong.TryParse(p.Id, out _))
            .Select(p => new PreviewEntry
            {
                Xp = p.Xp > 0 ? p.Xp : XpForLevel(p.Level),
                Level = p.Level > 0 ? p.Level : LevelFromXp(p.Xp)
            })
            .ToList();
    }

    private static List<PreviewEntry> ParseDynoPreview(string jsonData)
    {
        var entries = JsonSerializer.Deserialize<List<DynoEntry>>(jsonData,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (entries is null)
            return new List<PreviewEntry>();

        return entries
            .Where(e => ulong.TryParse(e.User, out _))
            .Select(e => new PreviewEntry
            {
                Xp = e.Xp > 0 ? e.Xp : XpForLevel(e.Level),
                Level = e.Level > 0 ? e.Level : LevelFromXp(e.Xp)
            })
            .ToList();
    }

    private static List<PreviewEntry> ParseCarlPreview(string csvData)
    {
        var results = new List<PreviewEntry>();
        var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines.Skip(1)) // skip header
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3) continue;
            if (!ulong.TryParse(parts[0], out _)) continue;

            int.TryParse(parts[1], out var level);
            long.TryParse(parts[2], out var xp);

            if (xp <= 0 && level > 0)
                xp = XpForLevel(level);
            if (level <= 0 && xp > 0)
                level = LevelFromXp(xp);

            results.Add(new PreviewEntry { Xp = xp, Level = level });
        }

        return results;
    }

    // ── DTOs ─────────────────────────────────────────────────────

    private sealed class Mee6Export
    {
        public List<Mee6Player> Players { get; set; }
    }

    private sealed class Mee6Player
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public long Xp { get; set; }
        public int Level { get; set; }
        public int Message_Count { get; set; }
    }

    private sealed class DynoEntry
    {
        public string User { get; set; }
        public int Level { get; set; }
        public long Xp { get; set; }
    }

    private sealed class ExportEntry
    {
        public string UserId { get; set; }
        public long Xp { get; set; }
        public int Level { get; set; }
    }

    private sealed class PreviewEntry
    {
        public long Xp { get; set; }
        public int Level { get; set; }
    }

    public sealed class MigrationPreviewResult
    {
        public int UserCount { get; set; }
        public long TotalXp { get; set; }
        public int HighestLevel { get; set; }
        public int LowestLevel { get; set; }
        public int AverageLevel { get; set; }
        public bool ParseError { get; set; }
    }
}
