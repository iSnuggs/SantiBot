#nullable disable
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class VoiceTextLinkService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    // Cache: VoiceChannelId → TextChannelId
    private readonly ConcurrentDictionary<ulong, ulong> _links = new();

    public VoiceTextLinkService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var allLinks = await uow.Set<VoiceTextLink>()
            .AsNoTracking()
            .ToListAsyncEF();

        foreach (var link in allLinks)
            _links[link.VoiceChannelId] = link.TextChannelId;

        _client.UserVoiceStateUpdated += OnVoiceStateUpdated;

        Log.Information("VoiceTextLink loaded {Count} links", allLinks.Count);
    }

    private Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var guildUser = user as SocketGuildUser;
                if (guildUser is null) return;

                // User left a linked voice channel
                if (before.VoiceChannel is not null && _links.TryGetValue(before.VoiceChannel.Id, out var leftTextId))
                {
                    var textChannel = guildUser.Guild.GetTextChannel(leftTextId);
                    if (textChannel is not null)
                    {
                        await textChannel.RemovePermissionOverwriteAsync(guildUser);
                    }
                }

                // User joined a linked voice channel
                if (after.VoiceChannel is not null && _links.TryGetValue(after.VoiceChannel.Id, out var joinedTextId))
                {
                    var textChannel = guildUser.Guild.GetTextChannel(joinedTextId);
                    if (textChannel is not null)
                    {
                        await textChannel.AddPermissionOverwriteAsync(guildUser,
                            new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "VoiceTextLink state update failed");
            }
        });

        return Task.CompletedTask;
    }

    public async Task<VoiceTextLink> AddLinkAsync(ulong guildId, ulong voiceChannelId, ulong textChannelId)
    {
        await using var uow = _db.GetDbContext();

        // Remove existing link for this voice channel if any
        var existing = await uow.Set<VoiceTextLink>()
            .FirstOrDefaultAsyncEF(l => l.GuildId == guildId && l.VoiceChannelId == voiceChannelId);
        if (existing is not null)
            uow.Set<VoiceTextLink>().Remove(existing);

        var link = new VoiceTextLink
        {
            GuildId = guildId,
            VoiceChannelId = voiceChannelId,
            TextChannelId = textChannelId,
        };

        uow.Set<VoiceTextLink>().Add(link);
        await uow.SaveChangesAsync();

        _links[voiceChannelId] = textChannelId;
        return link;
    }

    public async Task<bool> RemoveLinkAsync(ulong guildId, ulong voiceChannelId)
    {
        await using var uow = _db.GetDbContext();
        var link = await uow.Set<VoiceTextLink>()
            .FirstOrDefaultAsyncEF(l => l.GuildId == guildId && l.VoiceChannelId == voiceChannelId);

        if (link is null)
            return false;

        uow.Set<VoiceTextLink>().Remove(link);
        await uow.SaveChangesAsync();

        _links.TryRemove(voiceChannelId, out _);
        return true;
    }

    public async Task<List<VoiceTextLink>> GetLinksAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<VoiceTextLink>()
            .AsNoTracking()
            .Where(l => l.GuildId == guildId)
            .ToListAsyncEF();
    }
}
