using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantiBot.Db.Models;
using SantiBot.Services;

namespace SantiBot.Dashboard.Controllers;

/// <summary>
/// API endpoints for the dashboard audit log — tracks who changed what and when.
/// </summary>
[ApiController]
[Route("api/guilds/{guildId}/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly DbService _db;

    public AuditController(DbService db)
    {
        _db = db;
    }

    /// <summary>
    /// Get the last 50 audit log entries for this guild.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLog(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var entries = await ctx.GetTable<DashboardAuditLog>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Timestamp)
            .Take(50)
            .ToListAsyncLinqToDB();

        return Ok(entries.Select(e => new
        {
            id = e.Id,
            userId = e.UserId.ToString(),
            username = e.Username,
            action = e.Action,
            section = e.Section,
            timestamp = e.Timestamp,
        }));
    }
}
