using System.Security.Claims;
using SantiBot.Dashboard.Services;

namespace SantiBot.Dashboard.Middleware;

/// <summary>
/// Blocks access to guild-specific API endpoints unless the user has
/// "Manage Server" permission for that guild on Discord.
/// Only applies to routes matching /api/guilds/{guildId}/...
/// </summary>
public class GuildPermissionMiddleware
{
    private readonly RequestDelegate _next;

    public GuildPermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, GuildPermissionService permService)
    {
        var path = context.Request.Path.Value ?? "";

        // Only check guild-specific config routes (not /api/guilds list or /api/auth)
        if (!path.StartsWith("/api/guilds/") || !path.Contains("/config") && !path.Contains("/embeds"))
        {
            await _next(context);
            return;
        }

        // Extract guildId from the URL: /api/guilds/{guildId}/...
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3 || !ulong.TryParse(segments[2], out var guildId))
        {
            await _next(context);
            return;
        }

        // Get user ID from JWT claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !ulong.TryParse(userIdClaim, out var userId))
        {
            // No valid user — let the [Authorize] attribute handle this
            await _next(context);
            return;
        }

        // Check if the user can manage this guild
        var canManage = await permService.CanManageGuildAsync(userId, guildId);

        if (canManage == null)
        {
            // Discord token expired — tell the frontend to re-login
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"discord_token_expired","message":"Your Discord session has expired. Please log in again."}""");
            return;
        }

        if (canManage == false)
        {
            // User doesn't have permission for this guild
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"forbidden","message":"You don't have permission to manage this server."}""");
            return;
        }

        // Permission verified — continue to the controller
        await _next(context);
    }
}
