#nullable disable
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Modules.Utility.DashboardApi.LiveChat;

/// <summary>
/// Service for live chat preview in dashboard.
/// Captures messages from monitored channels and can push them to dashboard clients.
/// In production, this would connect to a SignalR hub.
///
/// SignalR Hub Methods to implement in dashboard:
///   JoinChannel(guildId, channelId)
///   LeaveChannel()
///   OnMessageReceived(message) -- push to connected clients
/// </summary>
public sealed class LiveChatService : INService, IReadyExecutor
{
    private readonly DiscordSocketClient _client;
    private readonly NonBlocking.ConcurrentDictionary<ulong, List<ChatMessage>> _recentMessages = new();

    public record ChatMessage(
        ulong MessageId,
        ulong ChannelId,
        ulong AuthorId,
        string AuthorName,
        string AvatarUrl,
        string Content,
        DateTime Timestamp);

    public LiveChatService(DiscordSocketClient client)
    {
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessageReceived;
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(SocketMessage msg)
    {
        if (msg is not SocketUserMessage userMsg || msg.Author.IsBot)
            return Task.CompletedTask;

        if (msg.Channel is not SocketTextChannel textChannel)
            return Task.CompletedTask;

        var chatMsg = new ChatMessage(
            msg.Id,
            msg.Channel.Id,
            msg.Author.Id,
            msg.Author.Username,
            msg.Author.GetAvatarUrl() ?? msg.Author.GetDefaultAvatarUrl(),
            msg.Content,
            msg.Timestamp.UtcDateTime);

        _recentMessages.AddOrUpdate(
            textChannel.Guild.Id,
            [chatMsg],
            (_, list) =>
            {
                list.Add(chatMsg);
                // Keep only last 100 messages per guild
                if (list.Count > 100)
                    list.RemoveRange(0, list.Count - 100);
                return list;
            });

        return Task.CompletedTask;
    }

    public List<ChatMessage> GetRecentMessages(ulong guildId, ulong channelId, int count = 50)
    {
        if (!_recentMessages.TryGetValue(guildId, out var messages))
            return [];

        return messages
            .Where(m => m.ChannelId == channelId)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .Reverse()
            .ToList();
    }

    public List<(ulong Id, string Name)> GetTextChannels(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null) return [];

        return guild.TextChannels
            .OrderBy(c => c.Position)
            .Select(c => (c.Id, c.Name))
            .ToList();
    }
}
