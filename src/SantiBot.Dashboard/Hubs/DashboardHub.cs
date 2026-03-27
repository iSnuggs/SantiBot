using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SantiBot.Dashboard.Hubs;

/// <summary>
/// SignalR hub for real-time dashboard updates.
///
/// How it works:
/// - When a user opens a guild's dashboard, they join a "group" for that guild
/// - When anyone saves settings, we broadcast the change to everyone in that group
/// - This means if two admins have the same server's dashboard open,
///   they both see changes instantly without refreshing
/// </summary>
[Authorize]
public class DashboardHub : Hub
{
    /// <summary>
    /// Called by the frontend when a user navigates to a guild's dashboard.
    /// Adds them to a SignalR group so they receive updates for that guild.
    /// </summary>
    public async Task JoinGuild(string guildId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"guild:{guildId}");
    }

    /// <summary>
    /// Called when the user leaves a guild's dashboard page.
    /// </summary>
    public async Task LeaveGuild(string guildId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"guild:{guildId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR automatically cleans up group memberships on disconnect
        await base.OnDisconnectedAsync(exception);
    }
}
