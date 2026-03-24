#nullable disable
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace SantiBot.Modules.Administration;

public sealed class WelcomeImageService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IHttpClientFactory _httpFactory;

    private readonly ConcurrentDictionary<ulong, WelcomeImageConfig> _configs = new();

    // Default fonts — ImageSharp bundled
    private FontFamily _fontFamily;
    private Font _titleFont;
    private Font _subtitleFont;
    private Font _nameFont;

    public WelcomeImageService(DbService db, DiscordSocketClient client, IHttpClientFactory httpFactory)
    {
        _db = db;
        _client = client;
        _httpFactory = httpFactory;
    }

    public async Task OnReadyAsync()
    {
        // Load fonts
        try
        {
            var collection = new FontCollection();
            // Try system fonts first, fall back to default
            if (SystemFonts.TryGet("Arial", out var family)
                || SystemFonts.TryGet("DejaVu Sans", out family)
                || SystemFonts.TryGet("Liberation Sans", out family))
            {
                _fontFamily = family;
            }
            else
            {
                _fontFamily = SystemFonts.Families.FirstOrDefault();
            }

            _titleFont = _fontFamily.CreateFont(28, FontStyle.Bold);
            _subtitleFont = _fontFamily.CreateFont(18, FontStyle.Regular);
            _nameFont = _fontFamily.CreateFont(36, FontStyle.Bold);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load fonts for welcome images");
        }

        await using var uow = _db.GetDbContext();
        var configs = await uow.Set<WelcomeImageConfig>()
            .AsNoTracking()
            .Where(c => c.Enabled)
            .ToListAsyncEF();

        foreach (var config in configs)
            _configs[config.GuildId] = config;

        _client.UserJoined += OnUserJoined;

        Log.Information("WelcomeImage loaded for {Count} guilds", configs.Count);
    }

    private Task OnUserJoined(SocketGuildUser user)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!_configs.TryGetValue(user.Guild.Id, out var config) || !config.Enabled)
                    return;

                if (!config.ChannelId.HasValue)
                    return;

                var channel = user.Guild.GetTextChannel(config.ChannelId.Value);
                if (channel is null)
                    return;

                var imageStream = await GenerateWelcomeImageAsync(config, user);
                if (imageStream is null)
                    return;

                await channel.SendFileAsync(imageStream, "welcome.png",
                    text: user.Mention);

                await imageStream.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send welcome image for {User}", user);
            }
        });

        return Task.CompletedTask;
    }

    public async Task<Stream> GenerateWelcomeImageAsync(WelcomeImageConfig config, SocketGuildUser user)
    {
        const int width = 800;
        const int height = 300;
        const int avatarSize = 128;

        // Parse accent color
        var accentHex = config.AccentColor ?? "0C95E9";
        if (!Rgba32.TryParseHex(accentHex, out var accentColor))
            accentColor = new Rgba32(12, 149, 233); // SantiBot blue

        // Create the base image
        using var image = new Image<Rgba32>(width, height);

        // Try to load custom background
        var hasBackground = false;
        if (!string.IsNullOrEmpty(config.BackgroundUrl))
        {
            try
            {
                var httpClient = _httpFactory.CreateClient();
                var bgBytes = await httpClient.GetByteArrayAsync(config.BackgroundUrl);
                using var bgImage = Image.Load<Rgba32>(bgBytes);
                bgImage.Mutate(x => x.Resize(width, height));
                image.Mutate(x => x.DrawImage(bgImage, 1f));
                hasBackground = true;
            }
            catch { }
        }

        if (!hasBackground)
        {
            // Default gradient background
            image.Mutate(x =>
            {
                var darkBg = new Rgba32(30, 30, 46);
                x.Fill(darkBg);
            });

            // Accent color stripe at bottom
            image.Mutate(x =>
            {
                var rect = new RectangularPolygon(0, height - 6, width, 6);
                x.Fill(accentColor, rect);
            });
        }

        // Semi-transparent overlay for text readability
        image.Mutate(x =>
        {
            var overlay = new Rgba32(0, 0, 0, 120);
            x.Fill(overlay, new RectangularPolygon(0, 0, width, height));
        });

        // Download and draw user avatar
        try
        {
            var avatarUrl = user.GetDisplayAvatarUrl(ImageFormat.Png, 256)
                ?? user.GetDefaultAvatarUrl();

            var httpClient = _httpFactory.CreateClient();
            var avatarBytes = await httpClient.GetByteArrayAsync(avatarUrl);
            using var avatar = Image.Load<Rgba32>(avatarBytes);
            avatar.Mutate(x => x.Resize(avatarSize, avatarSize));

            // Make it circular
            using var mask = new Image<Rgba32>(avatarSize, avatarSize);
            mask.Mutate(x => x.Fill(Color.White, new EllipsePolygon(avatarSize / 2f, avatarSize / 2f, avatarSize / 2f)));

            for (var y = 0; y < avatarSize; y++)
            {
                for (var px = 0; px < avatarSize; px++)
                {
                    if (mask[px, y].A == 0)
                        avatar[px, y] = new Rgba32(0, 0, 0, 0);
                }
            }

            // Draw avatar circle border
            var avatarX = (width - avatarSize) / 2;
            var avatarY = 20;

            image.Mutate(x =>
            {
                // Border circle
                var borderPen = Pens.Solid(accentColor, 3);
                x.Draw(borderPen, new EllipsePolygon(width / 2f, avatarY + avatarSize / 2f, avatarSize / 2f + 3));
            });

            image.Mutate(x => x.DrawImage(avatar, new SixLabors.ImageSharp.Point(avatarX, avatarY), 1f));
        }
        catch
        {
            // If avatar fails, continue without it
        }

        // Draw text
        if (_nameFont is not null)
        {
            var displayName = user.DisplayName ?? user.Username;
            var welcomeText = (config.WelcomeText ?? "Welcome to {server}!")
                .Replace("{user}", displayName)
                .Replace("{server}", user.Guild.Name)
                .Replace("{membercount}", user.Guild.MemberCount.ToString());

            var subtitleText = (config.SubtitleText ?? "You are member #{membercount}")
                .Replace("{user}", displayName)
                .Replace("{server}", user.Guild.Name)
                .Replace("{membercount}", user.Guild.MemberCount.ToString());

            var textOptions = new RichTextOptions(_nameFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Origin = new System.Numerics.Vector2(width / 2f, 165),
            };

            image.Mutate(x => x.DrawText(textOptions, displayName, Color.White));

            var welcomeOptions = new RichTextOptions(_titleFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Origin = new System.Numerics.Vector2(width / 2f, 210),
            };

            image.Mutate(x => x.DrawText(welcomeOptions, welcomeText, Color.White));

            var subOptions = new RichTextOptions(_subtitleFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Origin = new System.Numerics.Vector2(width / 2f, 250),
            };

            image.Mutate(x => x.DrawText(subOptions, subtitleText, new Rgba32(180, 180, 180)));
        }

        var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);
        ms.Position = 0;
        return ms;
    }

    // ── Public API ──

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var uow = _db.GetDbContext();
        var config = await uow.Set<WelcomeImageConfig>()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);

        if (config is null)
        {
            config = new WelcomeImageConfig { GuildId = guildId, Enabled = enabled };
            uow.Set<WelcomeImageConfig>().Add(config);
        }
        else
        {
            config.Enabled = enabled;
        }

        await uow.SaveChangesAsync();

        if (enabled)
            _configs[guildId] = config;
        else
            _configs.TryRemove(guildId, out _);
    }

    public async Task SetChannelAsync(ulong guildId, ulong? channelId)
    {
        var config = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<WelcomeImageConfig>().Attach(config);
        config.ChannelId = channelId;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetBackgroundAsync(ulong guildId, string url)
    {
        var config = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<WelcomeImageConfig>().Attach(config);
        config.BackgroundUrl = url;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetColorAsync(ulong guildId, string hexColor)
    {
        var config = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<WelcomeImageConfig>().Attach(config);
        config.AccentColor = hexColor.TrimStart('#');
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetTextAsync(ulong guildId, string welcomeText, string subtitleText = null)
    {
        var config = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<WelcomeImageConfig>().Attach(config);
        config.WelcomeText = welcomeText;
        if (subtitleText is not null)
            config.SubtitleText = subtitleText;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task<WelcomeImageConfig> GetConfigAsync(ulong guildId)
    {
        if (_configs.TryGetValue(guildId, out var config))
            return config;

        await using var uow = _db.GetDbContext();
        return await uow.Set<WelcomeImageConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);
    }

    private async Task<WelcomeImageConfig> GetOrCreateAsync(ulong guildId)
    {
        if (_configs.TryGetValue(guildId, out var cached))
            return cached;

        await using var uow = _db.GetDbContext();
        var config = await uow.Set<WelcomeImageConfig>()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);

        if (config is null)
        {
            config = new WelcomeImageConfig { GuildId = guildId };
            uow.Set<WelcomeImageConfig>().Add(config);
            await uow.SaveChangesAsync();
        }

        _configs[guildId] = config;
        return config;
    }
}
