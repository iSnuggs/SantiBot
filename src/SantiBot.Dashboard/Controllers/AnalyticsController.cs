using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantiBot.Db.Models;
using SantiBot.Services;

namespace SantiBot.Dashboard.Controllers;

/// <summary>
/// API endpoints for command usage analytics — see what commands your server uses most.
/// </summary>
[ApiController]
[Route("api/guilds/{guildId}/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly DbService _db;

    public AnalyticsController(DbService db)
    {
        _db = db;
    }

    /// <summary>
    /// Get the top 20 most-used commands (aggregated across all time).
    /// </summary>
    [HttpGet("commands")]
    public async Task<IActionResult> GetTopCommands(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var stats = await ctx.GetTable<CommandUsage>()
            .Where(x => x.GuildId == guildId)
            .GroupBy(x => x.CommandName)
            .Select(g => new
            {
                commandName = g.Key,
                totalUsage = g.Sum(x => x.UsageCount),
            })
            .OrderByDescending(x => x.totalUsage)
            .Take(20)
            .ToListAsyncLinqToDB();

        return Ok(stats);
    }

    /// <summary>
    /// Get daily command usage for the last N days (default 7) — for chart data.
    /// </summary>
    [HttpGet("commands/daily")]
    public async Task<IActionResult> GetDailyUsage(ulong guildId, [FromQuery] int days = 7)
    {
        if (days < 1) days = 1;
        if (days > 90) days = 90;

        var cutoff = DateTime.UtcNow.AddDays(-days).Date;

        await using var ctx = _db.GetDbContext();
        var stats = await ctx.GetTable<CommandUsage>()
            .Where(x => x.GuildId == guildId && x.Date >= cutoff)
            .GroupBy(x => x.Date)
            .Select(g => new
            {
                date = g.Key,
                totalUsage = g.Sum(x => x.UsageCount),
            })
            .OrderBy(x => x.date)
            .ToListAsyncLinqToDB();

        return Ok(stats);
    }
}
