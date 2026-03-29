#nullable disable
using System.Text.Json;

namespace SantiBot.Modules.Utility.DashboardApi.RoleHierarchy;

/// <summary>
/// Service for role hierarchy management.
/// Provides data methods that can be exposed via API controllers in the dashboard backend.
/// API Endpoints to create in dashboard:
///   GET  /api/guild/{guildId}/roles
///   PUT  /api/guild/{guildId}/roles/reorder
/// </summary>
public sealed class RoleHierarchyService : INService
{
    private readonly DiscordSocketClient _client;

    public RoleHierarchyService(DiscordSocketClient client)
    {
        _client = client;
    }

    public record RoleInfo(ulong Id, string Name, int Position, string Color, bool IsManaged, int MemberCount);

    public List<RoleInfo> GetRoles(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null) return [];

        return guild.Roles
            .OrderByDescending(r => r.Position)
            .Select(r => new RoleInfo(
                r.Id,
                r.Name,
                r.Position,
                r.Color.ToString(),
                r.IsManaged,
                r.Members.Count()))
            .ToList();
    }

    public async Task<bool> ReorderRolesAsync(ulong guildId, List<(ulong RoleId, int Position)> newOrder)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null) return false;

        try
        {
            var reorderProps = newOrder
                .Select(x => new ReorderRoleProperties(x.RoleId, x.Position))
                .ToArray();
            await guild.ReorderRolesAsync(reorderProps);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to reorder roles in guild {GuildId}", guildId);
            return false;
        }
    }

    public string GetRolesJson(ulong guildId)
    {
        var roles = GetRoles(guildId);
        return JsonSerializer.Serialize(roles);
    }
}
