using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class ConfessionService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IBotStrings _strings;
    private readonly ILocalization _localization;

    public ConfessionService(
        DbService db,
        DiscordSocketClient client,
        IMessageSenderService sender,
        IBotStrings strings,
        ILocalization localization)
    {
        _db = db;
        _client = client;
        _sender = sender;
        _strings = strings;
        _localization = localization;
    }

    public async Task<bool> EnableAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx
            .GetTable<ConfessionConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is not null)
        {
            await ctx.GetTable<ConfessionConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(x => new ConfessionConfig
                {
                    ChannelId = channelId,
                    Enabled = true
                });
        }
        else
        {
            await ctx.GetTable<ConfessionConfig>()
                .InsertAsync(() => new ConfessionConfig
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    Enabled = true,
                    NextConfessionNumber = 1
                });
        }

        return true;
    }

    public async Task<bool> DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var rows = await ctx.GetTable<ConfessionConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new ConfessionConfig
            {
                Enabled = false
            });

        return rows > 0;
    }

    public async Task<ConfessionConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx
            .GetTable<ConfessionConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task<int?> SubmitAsync(ulong guildId, ulong userId, string text)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx
            .GetTable<ConfessionConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Enabled);

        if (config is null)
            return null;

        var confessionNumber = config.NextConfessionNumber;

        // Increment the confession number
        await ctx.GetTable<ConfessionConfig>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(x => new ConfessionConfig
            {
                NextConfessionNumber = confessionNumber + 1
            });

        // Post the confession to the channel
        var guild = _client.GetGuild(guildId);
        if (guild is null)
            return null;

        var channel = guild.GetTextChannel(config.ChannelId);
        if (channel is null)
            return null;

        var culture = _localization.GetCultureInfo(guildId);

        var eb = _sender.CreateEmbed(guildId)
            .WithTitle(_strings.GetText(strs.confession_title(confessionNumber), culture))
            .WithDescription(text)
            .WithFooter(_strings.GetText(strs.confession_footer, culture))
            .WithCurrentTimestamp()
            .WithOkColor();

        await _sender.Response(channel).Embed(eb).SendAsync();

        return confessionNumber;
    }

    public async Task<bool> SetChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx
            .GetTable<ConfessionConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is not null)
        {
            await ctx.GetTable<ConfessionConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(x => new ConfessionConfig
                {
                    ChannelId = channelId
                });
        }
        else
        {
            await ctx.GetTable<ConfessionConfig>()
                .InsertAsync(() => new ConfessionConfig
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    Enabled = true,
                    NextConfessionNumber = 1
                });
        }

        return true;
    }
}
