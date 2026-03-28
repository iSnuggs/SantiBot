using Microsoft.AspNetCore.Mvc;

namespace SantiBot.Dashboard.Controllers;

/// <summary>
/// Public endpoint for bot status — no auth required.
/// Returns static placeholder data since the dashboard runs separately from the bot.
/// When the bot is wired up, this will query real stats.
/// </summary>
[ApiController]
[Route("api/status")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult GetStatus()
    {
        // Static placeholder data — will be replaced with real bot stats
        // once the dashboard↔bot communication is wired up.
        return Ok(new
        {
            online = true,
            guilds = 0,
            uptime = "0d 0h 0m",
            version = "1.0.0",
        });
    }
}
