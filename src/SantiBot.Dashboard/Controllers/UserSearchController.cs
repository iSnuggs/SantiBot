using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantiBot.Db.Models;
using SantiBot.Services;

namespace SantiBot.Dashboard.Controllers;

/// <summary>
/// API endpoint to search users by name — pulls from XP stats + DiscordUser tables.
/// </summary>
[ApiController]
[Route("api/guilds/{guildId}/users")]
[Authorize]
public class UserSearchController : ControllerBase
{
    private readonly DbService _db;

    public UserSearchController(DbService db)
    {
        _db = db;
    }

    /// <summary>
    /// Search users by partial username match. Returns XP, level, and warning count.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(ulong guildId, [FromQuery] string q = "")
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(Array.Empty<object>());

        await using var ctx = _db.GetDbContext();

        // Search DiscordUser table for matching usernames
        var matchingUsers = await ctx.GetTable<DiscordUser>()
            .Where(u => u.Username != null && u.Username.Contains(q))
            .Take(50)
            .ToListAsyncLinqToDB();

        var userIds = matchingUsers.Select(u => u.UserId).ToHashSet();

        // Also try parsing q as a user ID
        if (ulong.TryParse(q, out var directId))
            userIds.Add(directId);

        if (userIds.Count == 0)
            return Ok(Array.Empty<object>());

        // Get XP stats for matched users in this guild
        var xpStats = await ctx.GetTable<UserXpStats>()
            .Where(x => x.GuildId == guildId && userIds.Contains(x.UserId))
            .ToListAsyncLinqToDB();

        // Get warning counts for matched users in this guild
        var warnings = await ctx.GetTable<Warning>()
            .Where(w => w.GuildId == guildId && userIds.Contains(w.UserId))
            .GroupBy(w => w.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsyncLinqToDB();

        var warningMap = warnings.ToDictionary(w => w.UserId, w => w.Count);
        var userMap = matchingUsers.ToDictionary(u => u.UserId, u => u.Username ?? "Unknown");

        // Build results — include all matched users, even those without XP in this guild
        var results = userIds.Select(uid =>
        {
            var xp = xpStats.FirstOrDefault(x => x.UserId == uid);
            var totalXp = xp?.Xp ?? 0;
            // Simple level calculation: level = sqrt(xp / 100)
            var level = (int)Math.Floor(Math.Sqrt(totalXp / 100.0));

            return new
            {
                userId = uid.ToString(),
                username = userMap.GetValueOrDefault(uid, "Unknown"),
                xp = totalXp,
                level,
                warnings = warningMap.GetValueOrDefault(uid, 0),
            };
        })
        .OrderByDescending(x => x.xp)
        .Take(25)
        .ToList();

        return Ok(results);
    }
}
