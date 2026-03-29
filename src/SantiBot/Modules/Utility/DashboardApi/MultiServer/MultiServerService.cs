#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.DashboardApi.MultiServer;

/// <summary>
/// Multi-server view service.
/// API: GET /api/user/{userId}/servers/overview
/// Returns all servers the user has admin access to with stats.
/// </summary>
public sealed class MultiServerService : INService
{
    private readonly DiscordSocketClient _client;
    private readonly DbService _db;

    public MultiServerService(DiscordSocketClient client, DbService db)
    {
        _client = client;
        _db = db;
    }

    public record ServerOverview(
        ulong GuildId,
        string GuildName,
        string IconUrl,
        int MemberCount,
        int OnlineCount,
        int ChannelCount,
        int RoleCount,
        DateTime? BotJoinedAt);

    public List<ServerOverview> GetUserServers(ulong userId)
    {
        var results = new List<ServerOverview>();

        foreach (var guild in _client.Guilds)
        {
            var user = guild.GetUser(userId);
            if (user is null || !user.GuildPermissions.Administrator)
                continue;

            results.Add(new ServerOverview(
                guild.Id,
                guild.Name,
                guild.IconUrl,
                guild.MemberCount,
                guild.Users.Count(u => u.Status != UserStatus.Offline),
                guild.Channels.Count,
                guild.Roles.Count,
                guild.CurrentUser?.JoinedAt?.UtcDateTime));
        }

        return results;
    }

    public record GlobalStats(
        int TotalServers,
        int TotalMembers,
        int TotalChannels,
        TimeSpan Uptime);

    public GlobalStats GetGlobalStats()
    {
        return new GlobalStats(
            _client.Guilds.Count,
            _client.Guilds.Sum(g => g.MemberCount),
            _client.Guilds.Sum(g => g.Channels.Count),
            DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime());
    }
}
