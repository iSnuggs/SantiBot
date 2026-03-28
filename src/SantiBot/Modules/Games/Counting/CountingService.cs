using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games;

public sealed class CountingService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly IBotCreds _creds;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IBotStrings _strings;
    private readonly ILocalization _localization;

    public CountingService(
        DbService db,
        IBotCreds creds,
        DiscordSocketClient client,
        IMessageSenderService sender,
        IBotStrings strings,
        ILocalization localization)
    {
        _db = db;
        _creds = creds;
        _client = client;
        _sender = sender;
        _strings = strings;
        _localization = localization;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += HandleMessageAsync;
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(SocketMessage msg)
    {
        if (msg is not SocketUserMessage userMsg)
            return;

        if (msg.Author.IsBot || msg.Author.IsWebhook)
            return;

        if (msg.Channel is not ITextChannel textChannel)
            return;

        var guildId = textChannel.GuildId;

        await using var ctx = _db.GetDbContext();

        var config = await ctx
            .GetTable<CountingConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Enabled);

        if (config is null || config.ChannelId != textChannel.Id)
            return;

        // Check if the message is a number
        if (!int.TryParse(msg.Content.Trim(), out var number))
        {
            // Not a number — delete it
            try { await msg.DeleteAsync(); }
            catch { }
            return;
        }

        var expectedNext = config.CurrentCount + 1;

        // Check if same user is counting twice in a row
        if (msg.Author.Id == config.LastCountUserId)
        {
            try { await msg.DeleteAsync(); }
            catch { }

            var culture = _localization.GetCultureInfo(guildId);
            try
            {
                await _sender.Response(textChannel)
                    .Text(_strings.GetText(strs.counting_no_double, culture))
                    .SendAsync();
            }
            catch { }
            return;
        }

        // Check if the number is correct
        if (number != expectedNext)
        {
            // Wrong number — reset the count
            await ctx.GetTable<CountingConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(x => new CountingConfig
                {
                    CurrentCount = 0,
                    LastCountUserId = 0
                });

            var culture = _localization.GetCultureInfo(guildId);
            try
            {
                await _sender.Response(textChannel)
                    .Text(_strings.GetText(strs.counting_wrong(msg.Author.Mention, number, expectedNext), culture))
                    .SendAsync();
            }
            catch { }
            return;
        }

        // Correct number — update count
        await ctx.GetTable<CountingConfig>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(x => new CountingConfig
            {
                CurrentCount = number,
                LastCountUserId = msg.Author.Id
            });

        // React with a checkmark
        try { await userMsg.AddReactionAsync(new Emoji("\u2705")); }
        catch { }
    }

    public async Task SetChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx
            .GetTable<CountingConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is not null)
        {
            await ctx.GetTable<CountingConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(x => new CountingConfig
                {
                    ChannelId = channelId,
                    Enabled = true,
                    CurrentCount = 0,
                    LastCountUserId = 0
                });
        }
        else
        {
            await ctx.GetTable<CountingConfig>()
                .InsertAsync(() => new CountingConfig
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    Enabled = true,
                    CurrentCount = 0,
                    LastCountUserId = 0
                });
        }
    }

    public async Task<bool> ResetAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var rows = await ctx.GetTable<CountingConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new CountingConfig
            {
                CurrentCount = 0,
                LastCountUserId = 0
            });

        return rows > 0;
    }

    public async Task<CountingConfig?> GetStatusAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx
            .GetTable<CountingConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }
}
