using SantiBot.Db.Models;

namespace SantiBot.Modules.Xp.Services;

public interface IUserService
{
    Task<DiscordUser?> GetUserAsync(ulong userId);
}