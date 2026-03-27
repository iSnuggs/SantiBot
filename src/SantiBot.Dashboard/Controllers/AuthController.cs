using Microsoft.AspNetCore.Mvc;
using SantiBot.Dashboard.Services;

namespace SantiBot.Dashboard.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly DiscordOAuthService _oauth;
    private readonly JwtService _jwt;
    private readonly TokenStorageService _tokenStorage;
    private readonly IConfiguration _config;

    public AuthController(DiscordOAuthService oauth, JwtService jwt, TokenStorageService tokenStorage, IConfiguration config)
    {
        _oauth = oauth;
        _jwt = jwt;
        _tokenStorage = tokenStorage;
        _config = config;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var state = Guid.NewGuid().ToString("N");
        var url = _oauth.GetAuthorizationUrl(state);
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code))
            return BadRequest(new { error = "Missing authorization code" });

        var tokenResponse = await _oauth.ExchangeCodeAsync(code);
        if (tokenResponse is null)
            return BadRequest(new { error = "Failed to exchange code for token" });

        var user = await _oauth.GetUserAsync(tokenResponse.AccessToken);
        if (user is null)
            return BadRequest(new { error = "Failed to get user info" });

        if (!ulong.TryParse(user.Id, out var userId))
            return BadRequest(new { error = "Invalid user ID" });

        // Store the Discord token so we can fetch guilds and other data later
        _tokenStorage.StoreToken(
            userId,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresIn);

        var jwt = _jwt.GenerateToken(userId, user.Username, user.AvatarUrl);

        var frontendUrl = _config["Dashboard:FrontendUrl"] ?? "http://localhost:3000";
        return Redirect($"{frontendUrl}/auth/callback?token={jwt}");
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Me()
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var username = User.Identity?.Name;
        var avatar = User.FindFirst("avatar")?.Value;

        return Ok(new
        {
            userId = userId?.ToString(),
            username,
            avatar,
        });
    }
}
