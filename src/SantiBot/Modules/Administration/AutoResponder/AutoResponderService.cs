#nullable disable
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class AutoResponderService : IExecOnMessage, IReadyExecutor, INService
{
    // Run after automod but before commands
    public int Priority => int.MaxValue - 3;

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    // Cache: GuildId → list of active auto-responses
    private readonly ConcurrentDictionary<ulong, List<AutoResponse>> _responses = new();

    // Cooldown tracking: "guild:user:responseId" or "guild:channel:responseId" → last triggered time
    private readonly ConcurrentDictionary<string, DateTime> _cooldowns = new();

    public AutoResponderService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var allResponses = await uow.Set<AutoResponse>()
            .AsNoTracking()
            .Where(r => r.Enabled)
            .ToListAsyncEF();

        foreach (var group in allResponses.GroupBy(r => r.GuildId))
            _responses[group.Key] = group.ToList();

        Log.Information("AutoResponder loaded {Count} responses across {GuildCount} guilds",
            allResponses.Count, (object)_responses.Count);
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null || msg.Author is not IGuildUser user || user.IsBot)
            return false;

        if (!_responses.TryGetValue(guild.Id, out var responses) || responses.Count == 0)
            return false;

        foreach (var response in responses)
        {
            if (!IsTriggered(response, msg.Content))
                continue;

            // Check channel restrictions
            if (!string.IsNullOrEmpty(response.AllowedChannelIds))
            {
                var allowedChannels = response.AllowedChannelIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (!allowedChannels.Contains(msg.Channel.Id.ToString()))
                    continue;
            }

            // Check role restrictions
            if (!string.IsNullOrEmpty(response.AllowedRoleIds))
            {
                var allowedRoles = response.AllowedRoleIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (!user.RoleIds.Any(r => allowedRoles.Contains(r.ToString())))
                    continue;
            }

            // Check cooldowns
            if (response.UserCooldownSeconds > 0)
            {
                var key = $"{guild.Id}:u:{user.Id}:{response.Id}";
                if (_cooldowns.TryGetValue(key, out var lastTime)
                    && (DateTime.UtcNow - lastTime).TotalSeconds < response.UserCooldownSeconds)
                    continue;
                _cooldowns[key] = DateTime.UtcNow;
            }

            if (response.ChannelCooldownSeconds > 0)
            {
                var key = $"{guild.Id}:c:{msg.Channel.Id}:{response.Id}";
                if (_cooldowns.TryGetValue(key, out var lastTime)
                    && (DateTime.UtcNow - lastTime).TotalSeconds < response.ChannelCooldownSeconds)
                    continue;
                _cooldowns[key] = DateTime.UtcNow;
            }

            // Execute the response
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteResponseAsync(response, guild, user, msg);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "AutoResponder failed for response {ResponseId}", response.Id);
                }
            });

            // Don't block further processing — auto-responses are additive
            return false;
        }

        return false;
    }

    private static bool IsTriggered(AutoResponse response, string content)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(response.Trigger))
            return false;

        return response.TriggerType switch
        {
            AutoResponseTriggerType.Contains =>
                content.Contains(response.Trigger, StringComparison.OrdinalIgnoreCase),

            AutoResponseTriggerType.ExactMatch =>
                content.Equals(response.Trigger, StringComparison.OrdinalIgnoreCase),

            AutoResponseTriggerType.StartsWith =>
                content.StartsWith(response.Trigger, StringComparison.OrdinalIgnoreCase),

            AutoResponseTriggerType.Wildcard =>
                IsWildcardMatch(response.Trigger, content),

            AutoResponseTriggerType.Regex =>
                IsRegexMatch(response.Trigger, content),

            _ => false,
        };
    }

    private static bool IsWildcardMatch(string pattern, string content)
    {
        try
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(content, regexPattern, RegexOptions.IgnoreCase);
        }
        catch { return false; }
    }

    private static bool IsRegexMatch(string pattern, string content)
    {
        try
        {
            return Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        }
        catch { return false; }
    }

    private async Task ExecuteResponseAsync(AutoResponse response, IGuild guild, IGuildUser user, IUserMessage msg)
    {
        // Delete trigger message if configured
        if (response.DeleteTrigger)
        {
            try { await msg.DeleteAsync(); }
            catch { }
        }

        // Apply variable replacements
        var text = ApplyVariables(response.ResponseText, guild, user, msg);

        switch (response.ResponseType)
        {
            case AutoResponseType.Text:
                await msg.Channel.SendMessageAsync(text);
                break;

            case AutoResponseType.Embed:
                // For now, send as text — embed JSON parsing can be added later
                await msg.Channel.SendMessageAsync(text);
                break;

            case AutoResponseType.DM:
                try
                {
                    var dm = await user.CreateDMChannelAsync();
                    await dm.SendMessageAsync(text);
                }
                catch { }
                break;

            case AutoResponseType.Reaction:
                try
                {
                    if (Emote.TryParse(text, out var emote))
                        await msg.AddReactionAsync(emote);
                    else if (Emoji.TryParse(text, out var emoji))
                        await msg.AddReactionAsync(emoji);
                    else
                        await msg.AddReactionAsync(new Emoji(text));
                }
                catch { }
                break;
        }
    }

    private static string ApplyVariables(string text, IGuild guild, IGuildUser user, IUserMessage msg)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("{user}", user.Mention)
            .Replace("{user.name}", user.DisplayName)
            .Replace("{user.id}", user.Id.ToString())
            .Replace("{server}", guild.Name)
            .Replace("{server.id}", guild.Id.ToString())
            .Replace("{channel}", $"<#{msg.Channel.Id}>")
            .Replace("{channel.name}", msg.Channel.Name)
            .Replace("{membercount}", (guild as SocketGuild)?.MemberCount.ToString() ?? "?");
    }

    // ── Public API for Commands ──

    public async Task<AutoResponse> AddResponseAsync(ulong guildId, string trigger,
        AutoResponseTriggerType triggerType, string responseText, AutoResponseType responseType = AutoResponseType.Text)
    {
        await using var uow = _db.GetDbContext();
        var response = new AutoResponse
        {
            GuildId = guildId,
            Trigger = trigger,
            TriggerType = triggerType,
            ResponseText = responseText,
            ResponseType = responseType,
            Enabled = true,
        };

        uow.Set<AutoResponse>().Add(response);
        await uow.SaveChangesAsync();

        var guildResponses = _responses.GetOrAdd(guildId, _ => new());
        guildResponses.Add(response);

        return response;
    }

    public async Task<bool> RemoveResponseAsync(ulong guildId, int responseId)
    {
        await using var uow = _db.GetDbContext();
        var response = await uow.Set<AutoResponse>()
            .FirstOrDefaultAsyncEF(r => r.Id == responseId && r.GuildId == guildId);

        if (response is null)
            return false;

        uow.Set<AutoResponse>().Remove(response);
        await uow.SaveChangesAsync();

        if (_responses.TryGetValue(guildId, out var guildResponses))
            guildResponses.RemoveAll(r => r.Id == responseId);

        return true;
    }

    public async Task<bool> ToggleResponseAsync(ulong guildId, int responseId)
    {
        await using var uow = _db.GetDbContext();
        var response = await uow.Set<AutoResponse>()
            .FirstOrDefaultAsyncEF(r => r.Id == responseId && r.GuildId == guildId);

        if (response is null)
            return false;

        response.Enabled = !response.Enabled;
        await uow.SaveChangesAsync();

        if (_responses.TryGetValue(guildId, out var guildResponses))
        {
            if (response.Enabled)
            {
                if (!guildResponses.Any(r => r.Id == responseId))
                    guildResponses.Add(response);
            }
            else
            {
                guildResponses.RemoveAll(r => r.Id == responseId);
            }
        }

        return response.Enabled;
    }

    public async Task<AutoResponse> UpdateResponseAsync(ulong guildId, int responseId,
        bool? deleteTrigger = null, int? userCooldown = null, int? channelCooldown = null,
        string allowedChannels = null, string allowedRoles = null)
    {
        await using var uow = _db.GetDbContext();
        var response = await uow.Set<AutoResponse>()
            .FirstOrDefaultAsyncEF(r => r.Id == responseId && r.GuildId == guildId);

        if (response is null)
            return null;

        if (deleteTrigger.HasValue) response.DeleteTrigger = deleteTrigger.Value;
        if (userCooldown.HasValue) response.UserCooldownSeconds = userCooldown.Value;
        if (channelCooldown.HasValue) response.ChannelCooldownSeconds = channelCooldown.Value;
        if (allowedChannels is not null) response.AllowedChannelIds = allowedChannels;
        if (allowedRoles is not null) response.AllowedRoleIds = allowedRoles;

        await uow.SaveChangesAsync();

        // Update cache
        if (_responses.TryGetValue(guildId, out var guildResponses))
        {
            var cached = guildResponses.FirstOrDefault(r => r.Id == responseId);
            if (cached is not null)
            {
                guildResponses.Remove(cached);
                if (response.Enabled)
                    guildResponses.Add(response);
            }
        }

        return response;
    }

    public async Task<List<AutoResponse>> GetAllResponsesAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<AutoResponse>()
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .OrderBy(r => r.Id)
            .ToListAsyncEF();
    }
}
