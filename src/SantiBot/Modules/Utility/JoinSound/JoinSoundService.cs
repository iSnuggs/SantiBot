using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class JoinSoundService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;
    private readonly IHttpClientFactory _httpFactory;

    public JoinSoundService(
        DbService db,
        DiscordSocketClient client,
        IBotCreds creds,
        IHttpClientFactory httpFactory)
    {
        _db = db;
        _client = client;
        _creds = creds;
        _httpFactory = httpFactory;
    }

    public Task OnReadyAsync()
    {
        _client.UserVoiceStateUpdated += OnVoiceStateUpdatedAsync;
        return Task.CompletedTask;
    }

    private async Task OnVoiceStateUpdatedAsync(
        SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        try
        {
            // Only trigger when user joins a voice channel (not when leaving or moving)
            if (before.VoiceChannel is not null || after.VoiceChannel is null)
                return;

            var guild = after.VoiceChannel.Guild;
            var config = await GetConfigAsync(guild.Id);
            if (config is null || !config.Enabled)
                return;

            // Get user-specific sound or fall back to default
            var userSound = await GetUserSoundAsync(guild.Id, user.Id);
            var soundUrl = userSound?.SoundUrl;

            if (string.IsNullOrWhiteSpace(soundUrl))
                soundUrl = config.DefaultJoinUrl;

            if (string.IsNullOrWhiteSpace(soundUrl))
                return;

            // Connect to the voice channel, play the sound, then disconnect
            var voiceChannel = after.VoiceChannel;
            var audioClient = await voiceChannel.ConnectAsync();

            try
            {
                using var httpClient = _httpFactory.CreateClient();
                await using var audioStream = await httpClient.GetStreamAsync(soundUrl);
                await using var discordStream = audioClient.CreatePCMStream(Discord.Audio.AudioApplication.Mixed);

                // Read up to maxDuration worth of audio
                var buffer = new byte[3840]; // 20ms of audio at 48kHz stereo 16-bit
                var maxBytes = config.MaxDurationSeconds * 48000 * 2 * 2; // samples * channels * bytes per sample
                var totalRead = 0;

                int bytesRead;
                while ((bytesRead = await audioStream.ReadAsync(buffer)) > 0
                       && totalRead < maxBytes)
                {
                    await discordStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;
                }

                await discordStream.FlushAsync();
            }
            finally
            {
                await voiceChannel.DisconnectAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error playing join sound for user {UserId}", user.Id);
        }
    }

    public async Task<JoinSoundConfig?> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<JoinSoundConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);
    }

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<JoinSoundConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<JoinSoundConfig>()
                .InsertAsync(() => new JoinSoundConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                });
        }
        else
        {
            await ctx.GetTable<JoinSoundConfig>()
                .Where(x => x.GuildId == guildId)
                .UpdateAsync(x => new JoinSoundConfig { Enabled = enabled });
        }
    }

    public async Task SetDefaultSoundAsync(ulong guildId, string url)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<JoinSoundConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new JoinSoundConfig { DefaultJoinUrl = url });
    }

    public async Task SetMaxDurationAsync(ulong guildId, int seconds)
    {
        await using var ctx = _db.GetDbContext();
        await EnsureConfigAsync(ctx, guildId);
        await ctx.GetTable<JoinSoundConfig>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(x => new JoinSoundConfig { MaxDurationSeconds = seconds });
    }

    public async Task<UserJoinSound?> GetUserSoundAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserJoinSound>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);
    }

    public async Task SetUserSoundAsync(ulong guildId, ulong userId, string url)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<UserJoinSound>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);

        if (existing is null)
        {
            await ctx.GetTable<UserJoinSound>()
                .InsertAsync(() => new UserJoinSound
                {
                    GuildId = guildId,
                    UserId = userId,
                    SoundUrl = url,
                });
        }
        else
        {
            await ctx.GetTable<UserJoinSound>()
                .Where(x => x.GuildId == guildId && x.UserId == userId)
                .UpdateAsync(x => new UserJoinSound { SoundUrl = url });
        }
    }

    public async Task<bool> RemoveUserSoundAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<UserJoinSound>()
            .DeleteAsync(x => x.GuildId == guildId && x.UserId == userId);
        return deleted > 0;
    }

    private static async Task EnsureConfigAsync(SantiContext ctx, ulong guildId)
    {
        var exists = await ctx.GetTable<JoinSoundConfig>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId);

        if (!exists)
        {
            await ctx.GetTable<JoinSoundConfig>()
                .InsertAsync(() => new JoinSoundConfig { GuildId = guildId });
        }
    }
}
