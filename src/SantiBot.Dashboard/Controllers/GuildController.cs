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
    private readonly TokenStorageService _tokenStorage;

    public GuildController(DiscordOAuthService oauth, JwtService jwt, TokenStorageService tokenStorage)
    {
        _oauth = oauth;
        _jwt = jwt;
        _tokenStorage = tokenStorage;
    }

    /// <summary>
    /// Returns all Discord servers the logged-in user can manage.
    /// Uses the Discord token we saved when they logged in to call the Discord API.
    /// Only returns servers where the user has the "Manage Server" permission.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListGuilds()
    {
        var userId = _jwt.GetUserIdFromToken(User);
        if (userId is null)
            return Unauthorized();

        // Get the Discord token we stored when the user logged in
        var accessToken = _tokenStorage.GetAccessToken(userId.Value);
        if (accessToken is null)
            return Unauthorized(new
            {
                error = "discord_token_expired",
                message = "Your Discord session has expired. Please log in again.",
            });

        // Call Discord API to get the user's server list
        var allGuilds = await _oauth.GetUserGuildsAsync(accessToken);

        // Only show servers where the user has Manage Server permission
        var manageableGuilds = allGuilds
            .Where(g => g.CanManage)
            .Select(g => new
            {
                id = g.Id,
                name = g.Name,
                icon = g.Icon,
                iconUrl = g.IconUrl,
                owner = g.Owner,
                canManage = g.CanManage,
            })
            .OrderBy(g => g.name)
            .ToList();

        return Ok(manageableGuilds);
    }

    /// <summary>
    /// Returns details for a specific server, including basic info.
    /// Only accessible if the user has Manage Server permission for that guild.
    /// </summary>
    [HttpGet("{guildId}")]
    public async Task<IActionResult> GetGuild(ulong guildId)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        if (userId is null)
            return Unauthorized();

        var accessToken = _tokenStorage.GetAccessToken(userId.Value);
        if (accessToken is null)
            return Unauthorized(new
            {
                error = "discord_token_expired",
                message = "Your Discord session has expired. Please log in again.",
            });

        // Fetch all guilds and find the requested one
        var allGuilds = await _oauth.GetUserGuildsAsync(accessToken);
        var guild = allGuilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (guild is null)
            return NotFound(new { error = "Guild not found or you don't have access." });

        if (!guild.CanManage)
            return Forbid();

        return Ok(new
        {
            id = guild.Id,
            name = guild.Name,
            icon = guild.Icon,
            iconUrl = guild.IconUrl,
            owner = guild.Owner,
            canManage = guild.CanManage,
        });
    }
}
