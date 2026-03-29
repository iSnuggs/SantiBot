#nullable disable
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Modules.Games.Gamification;

/// <summary>
/// Bridges game events to Battle Pass XP.
/// Listens to messages and awards Battle Pass XP for activity.
/// Also provides a static method other services can call to award BP XP.
/// </summary>
public sealed class BattlePassBridge : INService, IExecOnMessage
{
    private readonly GamificationService _gf;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong, ulong), DateTime> _cooldowns = new();
    private static readonly TimeSpan MessageCooldown = TimeSpan.FromMinutes(1);

    // Static reference so other services can award BP XP without DI
    private static GamificationService _staticGf;

    public int Priority => 5;

    public BattlePassBridge(GamificationService gf)
    {
        _gf = gf;
        _staticGf = gf;
    }

    // Called on every message — award small BP XP for chatting
    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null || msg.Author.IsBot) return false;

        var key = (guild.Id, msg.Author.Id);
        if (_cooldowns.TryGetValue(key, out var last) && DateTime.UtcNow - last < MessageCooldown)
            return false;

        _cooldowns[key] = DateTime.UtcNow;
        await _gf.AddBattlePassXpAsync(msg.Author.Id, guild.Id, 5); // 5 BP XP per message (1 min cooldown)
        return false; // don't consume the message
    }

    /// <summary>Award Battle Pass XP from any service (static call)</summary>
    public static async Task AwardAsync(ulong userId, ulong guildId, long xp)
    {
        if (_staticGf is null) return;
        try { await _staticGf.AddBattlePassXpAsync(userId, guildId, xp); }
        catch { /* ignore — BP XP is non-critical */ }
    }
}
