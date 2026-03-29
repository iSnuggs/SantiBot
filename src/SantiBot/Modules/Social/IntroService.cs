#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class IntroService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    private static readonly string _defaultTemplate =
        "1. What should we call you?\n2. Where are you from?\n3. What are your hobbies?\n4. How did you find this server?";

    public IntroService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.UserJoined += OnUserJoined;
        return Task.CompletedTask;
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        try
        {
            if (user.IsBot) return;

            await using var ctx = _db.GetDbContext();
            var config = await ctx.GetTable<IntroConfig>()
                .Where(x => x.GuildId == user.Guild.Id && x.Enabled)
                .FirstOrDefaultAsyncLinqToDB();

            if (config is null) return;

            var template = string.IsNullOrWhiteSpace(config.Template) ? _defaultTemplate : config.Template;
            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(
                $"\U0001F44B **Welcome to {user.Guild.Name}!**\n\n" +
                $"Please answer these intro questions and we'll post your intro to the server:\n\n" +
                $"{template}\n\n" +
                $"Reply with your answers (one message, numbered) and I'll format them for you!");
        }
        catch { /* DMs may be disabled */ }
    }

    public async Task SetupAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<IntroConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is not null)
        {
            await ctx.GetTable<IntroConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.ChannelId, channelId)
                .Set(x => x.Enabled, true)
                .UpdateAsync();
        }
        else
        {
            await ctx.GetTable<IntroConfig>()
                .InsertAsync(() => new IntroConfig
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    Template = _defaultTemplate,
                    Enabled = true
                });
        }
    }

    public async Task SetTemplateAsync(ulong guildId, string template)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<IntroConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.Template, template)
            .UpdateAsync();
    }

    public async Task DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<IntroConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.Enabled, false)
            .UpdateAsync();
    }
}
