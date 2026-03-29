#nullable disable
namespace SantiBot.Db.Models;

public class BotCustomization : DbEntity
{
    public ulong GuildId { get; set; }

    // Currency customization
    public string CurrencyName { get; set; } = "Currency";
    public string CurrencyEmoji { get; set; } = "🥠";

    // XP customization
    public string XpName { get; set; } = "XP";
    public string XpEmoji { get; set; } = "⭐";

    // Embed customization
    public string EmbedColorHex { get; set; } = "#00E68A";
    public string EmbedFooterText { get; set; }
    public string EmbedThumbnailUrl { get; set; }

    // Response customization
    public string SuccessPrefix { get; set; } = "✅";
    public string ErrorPrefix { get; set; } = "❌";

    // Level-up customization
    public string LevelUpMessage { get; set; } = "🎉 {user} leveled up to **level {level}**!";
    public ulong LevelUpChannelId { get; set; }
    public bool LevelUpDm { get; set; }

    // Welcome customization
    public string WelcomeTitle { get; set; }
    public string WelcomeMessage { get; set; }
    public string WelcomeImageUrl { get; set; }

    // Goodbye customization
    public string GoodbyeMessage { get; set; }

    // Seasonal theming
    public string ActiveTheme { get; set; } = "default";
}

public class CustomEmbed : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ColorHex { get; set; }
    public string ThumbnailUrl { get; set; }
    public string ImageUrl { get; set; }
    public string FooterText { get; set; }
    public string AuthorName { get; set; }
    public string Fields { get; set; } // JSON: [{"name":"...", "value":"...", "inline":true}]
    public ulong CreatedBy { get; set; }
}

public class CustomCommand : DbEntity
{
    public ulong GuildId { get; set; }
    public string Trigger { get; set; }
    public string Response { get; set; }
    public string EmbedName { get; set; } // optional: use a saved embed
    public bool IsEmbed { get; set; }
    public ulong CreatedBy { get; set; }
    public int UseCount { get; set; }
}
