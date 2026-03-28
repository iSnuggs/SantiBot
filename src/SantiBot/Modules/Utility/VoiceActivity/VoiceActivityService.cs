namespace SantiBot.Modules.Utility;

public sealed class VoiceActivityService : INService
{
    // Discord embedded activity application IDs
    private static readonly Dictionary<string, (ulong AppId, string Name)> Activities = new()
    {
        ["youtube"] = (880218394199220334, "YouTube Watch Together"),
        ["poker"] = (755827207812677713, "Poker Night"),
        ["chess"] = (832012774040141894, "Chess in the Park"),
        ["sketch"] = (902271654783242291, "Sketch Heads"),
        ["letter"] = (879863686565621790, "Letter League"),
    };

    private readonly DiscordSocketClient _client;

    public VoiceActivityService(DiscordSocketClient client)
    {
        _client = client;
    }

    public static IReadOnlyDictionary<string, (ulong AppId, string Name)> GetActivities()
        => Activities;

    public static bool TryGetActivity(string key, out (ulong AppId, string Name) activity)
        => Activities.TryGetValue(key.ToLowerInvariant(), out activity);

    public async Task<string?> CreateActivityInviteAsync(IVoiceChannel voiceChannel, ulong applicationId)
    {
        try
        {
            // Create an invite with target_type=2 (embedded application)
            var invite = await voiceChannel.CreateInviteToApplicationAsync(applicationId, maxAge: 3600);
            return invite?.Url;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to create activity invite for app {AppId} in channel {ChannelId}",
                applicationId, voiceChannel.Id);
            return null;
        }
    }
}
