#nullable disable
namespace SantiBot.Modules.Administration.Services;

public sealed class MassActionService : INService
{
    private readonly DiscordSocketClient _client;

    public MassActionService(DiscordSocketClient client)
    {
        _client = client;
    }

    public async Task<int> MassBanByRoleAsync(IGuild guild, IRole role, string reason)
    {
        var users = await guild.GetUsersAsync();
        var targets = users.Where(u => u.RoleIds.Contains(role.Id)).ToList();
        var count = 0;

        foreach (var user in targets)
        {
            try
            {
                await guild.AddBanAsync(user, reason: reason);
                count++;
            }
            catch { /* skip users we can't ban */ }
        }

        return count;
    }

    public async Task<int> MassBanByJoinDateAsync(IGuild guild, DateTime before, string reason)
    {
        var users = await guild.GetUsersAsync();
        var targets = users.Where(u => u.JoinedAt.HasValue && u.JoinedAt.Value.UtcDateTime < before).ToList();
        var count = 0;

        foreach (var user in targets)
        {
            try
            {
                await guild.AddBanAsync(user, reason: reason);
                count++;
            }
            catch { }
        }

        return count;
    }

    public async Task<int> MassKickByRoleAsync(IGuild guild, IRole role, string reason)
    {
        var users = await guild.GetUsersAsync();
        var targets = users.Where(u => u.RoleIds.Contains(role.Id)).ToList();
        var count = 0;

        foreach (var user in targets)
        {
            try
            {
                await user.KickAsync(reason);
                count++;
            }
            catch { }
        }

        return count;
    }

    public async Task<int> MassMuteByRoleAsync(IGuild guild, IRole role, TimeSpan duration)
    {
        var users = await guild.GetUsersAsync();
        var targets = users.Where(u => u.RoleIds.Contains(role.Id)).ToList();
        var count = 0;

        foreach (var user in targets)
        {
            try
            {
                if (user is SocketGuildUser sgu)
                    await sgu.SetTimeOutAsync(duration);
                count++;
            }
            catch { }
        }

        return count;
    }
}
