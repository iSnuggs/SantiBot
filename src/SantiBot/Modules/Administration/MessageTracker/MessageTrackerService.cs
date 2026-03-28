using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Modules.Administration;

/// <summary>
/// Core moderation tool that caches recent messages so deleted and edited
/// content can be recovered by moderators. Works like Dyno's message tracking.
///
/// How it works:
/// - Caches the last 200 messages per channel in memory
/// - When a message is deleted, saves it to a "snipe" cache (last 25 per channel)
/// - When a message is edited, saves the before/after to an "edit snipe" cache
/// - Moderators use .snipe and .editsnipe to review changes
/// </summary>
public sealed class MessageTrackerService : INService, IReadyExecutor
{
    private readonly DiscordSocketClient _client;

    // Message cache: channelId -> queue of recent messages
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, BoundedQueue<CachedMessage>> _messageCache = new();

    // Snipe cache: channelId -> queue of deleted messages
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, BoundedQueue<DeletedMessage>> _deletedCache = new();

    // Edit snipe cache: channelId -> queue of edited messages
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, BoundedQueue<EditedMessage>> _editedCache = new();

    private const int MESSAGE_CACHE_SIZE = 200;
    private const int SNIPE_CACHE_SIZE = 25;

    public MessageTrackerService(DiscordSocketClient client)
    {
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessageReceived;
        _client.MessageDeleted += OnMessageDeleted;
        _client.MessageUpdated += OnMessageUpdated;
        _client.MessagesBulkDeleted += OnMessagesBulkDeleted;
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(SocketMessage msg)
    {
        // Don't cache bot messages or system messages
        if (msg.Author.IsBot || msg is not SocketUserMessage userMsg)
            return Task.CompletedTask;

        var cache = _messageCache.GetOrAdd(msg.Channel.Id, _ => new BoundedQueue<CachedMessage>(MESSAGE_CACHE_SIZE));
        cache.Enqueue(new CachedMessage
        {
            MessageId = msg.Id,
            AuthorId = msg.Author.Id,
            AuthorName = $"{msg.Author.Username}",
            Content = msg.Content,
            Attachments = msg.Attachments.Select(a => a.Url).ToList(),
            Timestamp = msg.Timestamp.UtcDateTime,
            ChannelId = msg.Channel.Id,
        });

        return Task.CompletedTask;
    }

    private Task OnMessageDeleted(Cacheable<IMessage, ulong> msgCache, Cacheable<IMessageChannel, ulong> chCache)
    {
        _ = Task.Run(() =>
        {
            CachedMessage? cached = null;

            // Try to get from Discord's cache first
            if (msgCache.HasValue && msgCache.Value is IUserMessage discordMsg && !discordMsg.Author.IsBot)
            {
                cached = new CachedMessage
                {
                    MessageId = discordMsg.Id,
                    AuthorId = discordMsg.Author.Id,
                    AuthorName = discordMsg.Author.Username,
                    Content = discordMsg.Content,
                    Attachments = discordMsg.Attachments.Select(a => a.Url).ToList(),
                    Timestamp = discordMsg.Timestamp.UtcDateTime,
                    ChannelId = chCache.Id,
                };
            }

            // Fall back to our own cache
            if (cached is null && _messageCache.TryGetValue(chCache.Id, out var queue))
            {
                cached = queue.FirstOrDefault(m => m.MessageId == msgCache.Id);
            }

            if (cached is null)
                return;

            var snipeCache = _deletedCache.GetOrAdd(chCache.Id, _ => new BoundedQueue<DeletedMessage>(SNIPE_CACHE_SIZE));
            snipeCache.Enqueue(new DeletedMessage
            {
                Original = cached,
                DeletedAt = DateTime.UtcNow,
            });
        });

        return Task.CompletedTask;
    }

    private Task OnMessageUpdated(
        Cacheable<IMessage, ulong> before,
        SocketMessage after,
        ISocketMessageChannel channel)
    {
        _ = Task.Run(() =>
        {
            if (after.Author.IsBot || after is not SocketUserMessage)
                return;

            string? oldContent = null;

            // Try Discord cache for the old version
            if (before.HasValue)
                oldContent = before.Value.Content;

            // Try our cache
            if (oldContent is null && _messageCache.TryGetValue(channel.Id, out var queue))
            {
                var cached = queue.FirstOrDefault(m => m.MessageId == before.Id);
                oldContent = cached?.Content;
            }

            if (oldContent is null || oldContent == after.Content)
                return;

            var editCache = _editedCache.GetOrAdd(channel.Id, _ => new BoundedQueue<EditedMessage>(SNIPE_CACHE_SIZE));
            editCache.Enqueue(new EditedMessage
            {
                MessageId = after.Id,
                AuthorId = after.Author.Id,
                AuthorName = after.Author.Username,
                OldContent = oldContent,
                NewContent = after.Content,
                EditedAt = DateTime.UtcNow,
                ChannelId = channel.Id,
            });

            // Update our cache with the new content
            if (_messageCache.TryGetValue(channel.Id, out var msgQueue))
            {
                var existing = msgQueue.FirstOrDefault(m => m.MessageId == after.Id);
                if (existing is not null)
                    existing.Content = after.Content;
            }
        });

        return Task.CompletedTask;
    }

    private Task OnMessagesBulkDeleted(
        IReadOnlyCollection<Cacheable<IMessage, ulong>> messages,
        Cacheable<IMessageChannel, ulong> channel)
    {
        _ = Task.Run(() =>
        {
            var snipeCache = _deletedCache.GetOrAdd(channel.Id, _ => new BoundedQueue<DeletedMessage>(SNIPE_CACHE_SIZE));

            foreach (var msgCache in messages)
            {
                CachedMessage? cached = null;

                if (msgCache.HasValue && msgCache.Value is IUserMessage discordMsg && !discordMsg.Author.IsBot)
                {
                    cached = new CachedMessage
                    {
                        MessageId = discordMsg.Id,
                        AuthorId = discordMsg.Author.Id,
                        AuthorName = discordMsg.Author.Username,
                        Content = discordMsg.Content,
                        Attachments = discordMsg.Attachments.Select(a => a.Url).ToList(),
                        Timestamp = discordMsg.Timestamp.UtcDateTime,
                        ChannelId = channel.Id,
                    };
                }

                if (cached is null && _messageCache.TryGetValue(channel.Id, out var queue))
                    cached = queue.FirstOrDefault(m => m.MessageId == msgCache.Id);

                if (cached is not null)
                {
                    snipeCache.Enqueue(new DeletedMessage
                    {
                        Original = cached,
                        DeletedAt = DateTime.UtcNow,
                    });
                }
            }
        });

        return Task.CompletedTask;
    }

    // ── Public API for commands ──

    /// <summary>Get the most recently deleted messages in a channel.</summary>
    public IReadOnlyList<DeletedMessage> GetDeletedMessages(ulong channelId, int count = 5)
    {
        if (!_deletedCache.TryGetValue(channelId, out var cache))
            return Array.Empty<DeletedMessage>();

        return cache.TakeLast(count).Reverse().ToList();
    }

    /// <summary>Get the most recently edited messages in a channel.</summary>
    public IReadOnlyList<EditedMessage> GetEditedMessages(ulong channelId, int count = 5)
    {
        if (!_editedCache.TryGetValue(channelId, out var cache))
            return Array.Empty<EditedMessage>();

        return cache.TakeLast(count).Reverse().ToList();
    }

    /// <summary>Get deleted messages by a specific user in a channel.</summary>
    public IReadOnlyList<DeletedMessage> GetDeletedByUser(ulong channelId, ulong userId, int count = 5)
    {
        if (!_deletedCache.TryGetValue(channelId, out var cache))
            return Array.Empty<DeletedMessage>();

        return cache.Where(m => m.Original.AuthorId == userId)
                     .TakeLast(count).Reverse().ToList();
    }
}

// ── Data models ──

public class CachedMessage
{
    public ulong MessageId { get; set; }
    public ulong AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Content { get; set; } = "";
    public List<string> Attachments { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public ulong ChannelId { get; set; }
}

public class DeletedMessage
{
    public CachedMessage Original { get; set; } = new();
    public DateTime DeletedAt { get; set; }
}

public class EditedMessage
{
    public ulong MessageId { get; set; }
    public ulong AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string OldContent { get; set; } = "";
    public string NewContent { get; set; } = "";
    public DateTime EditedAt { get; set; }
    public ulong ChannelId { get; set; }
}

/// <summary>Thread-safe bounded queue that evicts oldest items when full.</summary>
public class BoundedQueue<T>
{
    private readonly Queue<T> _queue;
    private readonly int _maxSize;
    private readonly object _lock = new();

    public BoundedQueue(int maxSize)
    {
        _maxSize = maxSize;
        _queue = new Queue<T>(maxSize);
    }

    public void Enqueue(T item)
    {
        lock (_lock)
        {
            while (_queue.Count >= _maxSize)
                _queue.Dequeue();
            _queue.Enqueue(item);
        }
    }

    public T? FirstOrDefault(Func<T, bool> predicate)
    {
        lock (_lock)
            return _queue.FirstOrDefault(predicate);
    }

    public IEnumerable<T> Where(Func<T, bool> predicate)
    {
        lock (_lock)
            return _queue.Where(predicate).ToList();
    }

    public IEnumerable<T> TakeLast(int count)
    {
        lock (_lock)
            return _queue.TakeLast(count).ToList();
    }
}
