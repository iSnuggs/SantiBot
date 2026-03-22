using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantiBot.Dashboard.Services;

namespace SantiBot.Dashboard.Controllers;

[ApiController]
[Route("api/guilds")]
[Authorize]
public class GuildController : ControllerBase
{
    private readonly DiscordOAuthService _oauth;
    private readonly JwtService _jwt;

    public GuildController(DiscordOAuthService oauth, JwtService jwt)
    {
        _oauth = oauth;
        _jwt = jwt;
    }

    /// <summary>
    /// List guilds the authenticated user can manage.
    /// Note: This requires the user's Discord access token, which we'd store in a cache.
    /// For now, this returns a placeholder indicating the endpoint structure.
    /// In production, store Discord tokens server-side keyed by user ID.
    /// </summary>
    [HttpGet]
    public IActionResult ListGuilds()
    {
        var userId = _jwt.GetUserIdFromToken(User);
        if (userId is null)
            return Unauthorized();

        // In production, retrieve stored Discord access token for this user
        // and call _oauth.GetUserGuildsAsync(accessToken)
        return Ok(new
        {
            message = "Guild list endpoint ready. Requires Discord token storage integration.",
            userId = userId.ToString(),
        });
    }

    [HttpGet("{guildId}")]
    public IActionResult GetGuild(ulong guildId)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        if (userId is null)
            return Unauthorized();

        return Ok(new
        {
            guildId = guildId.ToString(),
            message = "Guild detail endpoint ready. Will return channels, roles, and settings.",
        });
    }
}
