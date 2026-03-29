#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.Customization;

public sealed class CustomizationService : INService
{
    private readonly DbService _db;

    public CustomizationService(DbService db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════════════════
    //  SEASONAL THEMES
    // ═══════════════════════════════════════════════════════════

    public static readonly Dictionary<string, (string Name, string Emoji, string Color, string Desc)> Themes = new()
    {
        ["default"]    = ("Default",     "🎮", "#00E68A", "The standard SantiBot theme"),
        ["dark"]       = ("Dark Mode",   "🌑", "#1a1a2e", "Sleek dark theme"),
        ["neon"]       = ("Neon",        "💜", "#FF00FF", "Bright neon cyberpunk vibes"),
        ["ocean"]      = ("Ocean",       "🌊", "#006994", "Deep sea blue theme"),
        ["forest"]     = ("Forest",      "🌲", "#228B22", "Nature green theme"),
        ["sunset"]     = ("Sunset",      "🌅", "#FF6B35", "Warm sunset orange"),
        ["galaxy"]     = ("Galaxy",      "🌌", "#7B2FBE", "Cosmic purple theme"),
        ["cherry"]     = ("Cherry",      "🌸", "#FFB7C5", "Soft cherry blossom pink"),
        ["halloween"]  = ("Halloween",   "🎃", "#FF6600", "Spooky orange and black"),
        ["christmas"]  = ("Christmas",   "🎄", "#CC0000", "Festive red and green"),
        ["valentine"]  = ("Valentine",   "💕", "#FF1493", "Love pink theme"),
        ["spring"]     = ("Spring",      "🌷", "#77DD77", "Fresh spring green"),
        ["summer"]     = ("Summer",      "☀️", "#FFD700", "Bright summer gold"),
        ["autumn"]     = ("Autumn",      "🍂", "#CD853F", "Warm autumn brown"),
        ["winter"]     = ("Winter",      "❄️", "#ADD8E6", "Cool winter ice blue"),
        ["retro"]      = ("Retro",       "📺", "#FF4500", "80s retro orange"),
        ["royal"]      = ("Royal",       "👑", "#4B0082", "Royal indigo"),
        ["fire"]       = ("Fire",        "🔥", "#FF4500", "Blazing fire red"),
        ["ice"]        = ("Ice",         "🧊", "#B0E0E6", "Frozen ice blue"),
        ["gold"]       = ("Gold",        "✨", "#FFD700", "Premium gold theme"),
    };

    // ═══════════════════════════════════════════════════════════
    //  BOT CUSTOMIZATION
    // ═══════════════════════════════════════════════════════════

    public async Task<BotCustomization> GetOrCreateAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<BotCustomization>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is not null) return config;

        config = new BotCustomization { GuildId = guildId };
        ctx.Add(config);
        await ctx.SaveChangesAsync();
        return config;
    }

    public async Task SetCurrencyAsync(ulong guildId, string name, string emoji)
    {
        await using var ctx = _db.GetDbContext();
        var config = await GetOrCreateAsync(guildId);
        await ctx.GetTable<BotCustomization>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(_ => new BotCustomization { CurrencyName = name, CurrencyEmoji = emoji });
    }

    public async Task SetXpAsync(ulong guildId, string name, string emoji)
    {
        await using var ctx = _db.GetDbContext();
        var config = await GetOrCreateAsync(guildId);
        await ctx.GetTable<BotCustomization>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(_ => new BotCustomization { XpName = name, XpEmoji = emoji });
    }

    public async Task SetEmbedColorAsync(ulong guildId, string hex)
    {
        await using var ctx = _db.GetDbContext();
        var config = await GetOrCreateAsync(guildId);
        await ctx.GetTable<BotCustomization>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(_ => new BotCustomization { EmbedColorHex = hex });
    }

    public async Task SetLevelUpAsync(ulong guildId, string message, ulong channelId, bool dm)
    {
        await using var ctx = _db.GetDbContext();
        var config = await GetOrCreateAsync(guildId);
        await ctx.GetTable<BotCustomization>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(_ => new BotCustomization
            {
                LevelUpMessage = message,
                LevelUpChannelId = channelId,
                LevelUpDm = dm,
            });
    }

    public async Task SetWelcomeAsync(ulong guildId, string title, string message, string imageUrl)
    {
        await using var ctx = _db.GetDbContext();
        var config = await GetOrCreateAsync(guildId);
        await ctx.GetTable<BotCustomization>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(_ => new BotCustomization
            {
                WelcomeTitle = title,
                WelcomeMessage = message,
                WelcomeImageUrl = imageUrl,
            });
    }

    public async Task SetThemeAsync(ulong guildId, string theme)
    {
        await using var ctx = _db.GetDbContext();
        var config = await GetOrCreateAsync(guildId);
        await ctx.GetTable<BotCustomization>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(_ => new BotCustomization { ActiveTheme = theme });
    }

    // ═══════════════════════════════════════════════════════════
    //  CUSTOM EMBEDS
    // ═══════════════════════════════════════════════════════════

    public async Task<CustomEmbed> SaveEmbedAsync(CustomEmbed embed)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<CustomEmbed>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == embed.GuildId && x.Name == embed.Name);

        if (existing is not null)
        {
            await ctx.GetTable<CustomEmbed>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new CustomEmbed
                {
                    Title = embed.Title,
                    Description = embed.Description,
                    ColorHex = embed.ColorHex,
                    ThumbnailUrl = embed.ThumbnailUrl,
                    ImageUrl = embed.ImageUrl,
                    FooterText = embed.FooterText,
                    Fields = embed.Fields,
                });
            return existing;
        }

        ctx.Add(embed);
        await ctx.SaveChangesAsync();
        return embed;
    }

    public async Task<CustomEmbed> GetEmbedAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<CustomEmbed>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId &&
                x.Name.ToLower() == name.ToLower());
    }

    public async Task<List<CustomEmbed>> GetEmbedsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<CustomEmbed>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> DeleteEmbedAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<CustomEmbed>()
            .Where(x => x.GuildId == guildId && x.Name.ToLower() == name.ToLower())
            .DeleteAsync();
        return deleted > 0;
    }

    // ═══════════════════════════════════════════════════════════
    //  CUSTOM COMMANDS
    // ═══════════════════════════════════════════════════════════

    public async Task<CustomCommand> SaveCommandAsync(CustomCommand cmd)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<CustomCommand>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == cmd.GuildId &&
                x.Trigger.ToLower() == cmd.Trigger.ToLower());

        if (existing is not null)
        {
            await ctx.GetTable<CustomCommand>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new CustomCommand { Response = cmd.Response, IsEmbed = cmd.IsEmbed, EmbedName = cmd.EmbedName });
            return existing;
        }

        ctx.Add(cmd);
        await ctx.SaveChangesAsync();
        return cmd;
    }

    public async Task<CustomCommand> GetCommandAsync(ulong guildId, string trigger)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<CustomCommand>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId &&
                x.Trigger.ToLower() == trigger.ToLower());
    }

    public async Task<List<CustomCommand>> GetCommandsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<CustomCommand>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.UseCount)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> DeleteCommandAsync(ulong guildId, string trigger)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<CustomCommand>()
            .Where(x => x.GuildId == guildId && x.Trigger.ToLower() == trigger.ToLower())
            .DeleteAsync();
        return deleted > 0;
    }

    public async Task IncrementCommandUseAsync(int commandId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<CustomCommand>()
            .Where(x => x.Id == commandId)
            .UpdateAsync(x => new CustomCommand { UseCount = x.UseCount + 1 });
    }
}
