#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.DashboardApi.WhiteLabel;

/// <summary>
/// White-label mode -- custom bot branding per server.
/// API: GET/PUT /api/guild/{guildId}/branding
/// </summary>
public sealed class WhiteLabelService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IHttpClientFactory _http;

    public WhiteLabelService(DbService db, DiscordSocketClient client, IHttpClientFactory http)
    {
        _db = db;
        _client = client;
        _http = http;
    }

    public async Task<WhiteLabelConfig> GetConfigAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<WhiteLabelConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task SetNameAsync(ulong guildId, string name)
    {
        await using var uow = _db.GetDbContext();
        var existing = await uow.GetTable<WhiteLabelConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await uow.GetTable<WhiteLabelConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new WhiteLabelConfig { BotName = name });
        }
        else
        {
            await uow.GetTable<WhiteLabelConfig>()
                .InsertAsync(() => new WhiteLabelConfig
                {
                    GuildId = guildId,
                    BotName = name
                });
        }

        // Try to change the bot's nickname in the guild
        try
        {
            var guild = _client.GetGuild(guildId);
            var botUser = guild?.CurrentUser;
            if (botUser is not null)
                await botUser.ModifyAsync(x => x.Nickname = name);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not set bot nickname in guild {GuildId}", guildId);
        }
    }

    public async Task SetAvatarAsync(ulong guildId, string avatarUrl)
    {
        await using var uow = _db.GetDbContext();
        var existing = await uow.GetTable<WhiteLabelConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await uow.GetTable<WhiteLabelConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new WhiteLabelConfig { AvatarUrl = avatarUrl });
        }
        else
        {
            await uow.GetTable<WhiteLabelConfig>()
                .InsertAsync(() => new WhiteLabelConfig
                {
                    GuildId = guildId,
                    AvatarUrl = avatarUrl
                });
        }
    }

    public async Task SetColorAsync(ulong guildId, string hexColor)
    {
        await using var uow = _db.GetDbContext();
        var existing = await uow.GetTable<WhiteLabelConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await uow.GetTable<WhiteLabelConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new WhiteLabelConfig { PrimaryColor = hexColor });
        }
        else
        {
            await uow.GetTable<WhiteLabelConfig>()
                .InsertAsync(() => new WhiteLabelConfig
                {
                    GuildId = guildId,
                    PrimaryColor = hexColor
                });
        }
    }

    public async Task ResetAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        await uow.GetTable<WhiteLabelConfig>()
            .Where(x => x.GuildId == guildId)
            .DeleteAsync();

        try
        {
            var guild = _client.GetGuild(guildId);
            var botUser = guild?.CurrentUser;
            if (botUser is not null)
                await botUser.ModifyAsync(x => x.Nickname = null);
        }
        catch { }
    }
}
