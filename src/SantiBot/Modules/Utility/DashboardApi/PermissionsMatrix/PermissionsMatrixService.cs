#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Utility.DashboardApi.PermissionsMatrix;

/// <summary>
/// Permissions matrix API.
/// GET /api/guild/{guildId}/permissions/matrix
/// Returns all commands x all roles = allow/deny grid.
/// </summary>
public sealed class PermissionsMatrixService : INService
{
    private readonly DiscordSocketClient _client;
    private readonly DbService _db;

    public PermissionsMatrixService(DiscordSocketClient client, DbService db)
    {
        _client = client;
        _db = db;
    }

    public record PermissionEntry(string Command, ulong RoleId, string RoleName, bool IsAllowed);

    public async Task<List<PermissionEntry>> GetMatrixAsync(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null) return [];

        await using var uow = _db.GetDbContext();
        var guildPerms = await uow.GetTable<Permissionv2>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        var result = new List<PermissionEntry>();
        foreach (var perm in guildPerms)
        {
            if (perm.PrimaryTarget == PrimaryPermissionType.Role && perm.PrimaryTargetId != 0)
            {
                var role = guild.GetRole(perm.PrimaryTargetId);
                result.Add(new PermissionEntry(
                    perm.SecondaryTargetName ?? "*",
                    perm.PrimaryTargetId,
                    role?.Name ?? "Unknown",
                    perm.State));
            }
        }

        return result;
    }

    public string GetMatrixJson(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null) return "[]";

        var roles = guild.Roles
            .OrderByDescending(r => r.Position)
            .Select(r => new { r.Id, r.Name, r.Position })
            .ToList();

        // Return role data for the dashboard to display
        return JsonSerializer.Serialize(new { roles });
    }
}
