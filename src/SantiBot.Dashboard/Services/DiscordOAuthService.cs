using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SantiBot.Dashboard.Services;

public class DiscordOAuthService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    private string ClientId => Environment.GetEnvironmentVariable("DISCORD_DASHBOARD_CLIENT_ID")
        ?? _config["Discord:ClientId"] ?? "";
    private string ClientSecret => Environment.GetEnvironmentVariable("DISCORD_DASHBOARD_CLIENT_SECRET")
        ?? _config["Discord:ClientSecret"] ?? "";
    private string RedirectUri => _config["Discord:RedirectUri"] ?? "http://localhost:5000/api/auth/callback";

    public DiscordOAuthService(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _httpFactory = httpFactory;
    }

    public string GetAuthorizationUrl(string state)
    {
        var scopes = "identify guilds";
        return $"https://discord.com/api/oauth2/authorize" +
               $"?client_id={ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
               $"&response_type=code" +
               $"&scope={Uri.EscapeDataString(scopes)}" +
               $"&state={state}";
    }

    public async Task<DiscordTokenResponse?> ExchangeCodeAsync(string code)
    {
        using var http = _httpFactory.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = RedirectUri,
        });

        var response = await http.PostAsync("https://discord.com/api/oauth2/token", content);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DiscordTokenResponse>(json);
    }

    public async Task<DiscordUser?> GetUserAsync(string accessToken)
    {
        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await http.GetAsync("https://discord.com/api/v10/users/@me");
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DiscordUser>(json);
    }

    public async Task<List<DiscordPartialGuild>> GetUserGuildsAsync(string accessToken)
    {
        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await http.GetAsync("https://discord.com/api/v10/users/@me/guilds");
        if (!response.IsSuccessStatusCode)
            return new();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DiscordPartialGuild>>(json) ?? new();
    }
}

public class DiscordTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "";
}

public class DiscordUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    public string AvatarUrl => Avatar is not null
        ? $"https://cdn.discordapp.com/avatars/{Id}/{Avatar}.png"
        : $"https://cdn.discordapp.com/embed/avatars/{int.Parse(Id) % 5}.png";
}

public class DiscordPartialGuild
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("owner")]
    public bool Owner { get; set; }

    [JsonPropertyName("permissions")]
    public string Permissions { get; set; } = "0";

    public bool CanManage =>
        Owner || (ulong.TryParse(Permissions, out var p) && (p & 0x20) != 0); // MANAGE_GUILD

    public string? IconUrl => Icon is not null
        ? $"https://cdn.discordapp.com/icons/{Id}/{Icon}.png"
        : null;
}
