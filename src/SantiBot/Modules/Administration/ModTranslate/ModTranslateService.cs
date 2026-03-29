#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Net.Http;
using System.Text.Json;

namespace SantiBot.Modules.Administration.Services;

public sealed class ModTranslateService : INService
{
    private readonly DbService _db;
    private readonly IHttpClientFactory _httpFactory;

    public ModTranslateService(DbService db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    public async Task<string> TranslateAsync(string text, string targetLang)
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair=autodetect|{targetLang}";
            var response = await http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            var translated = json.RootElement.GetProperty("responseData").GetProperty("translatedText").GetString();
            return translated;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetConfigAsync(ulong guildId, string targetLang, bool enabled)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<ModTranslateConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<ModTranslateConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new ModTranslateConfig { TargetLanguage = targetLang, IsEnabled = enabled });
        }
        else
        {
            await ctx.GetTable<ModTranslateConfig>()
                .InsertAsync(() => new ModTranslateConfig
                {
                    GuildId = guildId,
                    TargetLanguage = targetLang,
                    IsEnabled = enabled
                });
        }
    }

    public async Task<ModTranslateConfig> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ModTranslateConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }
}
