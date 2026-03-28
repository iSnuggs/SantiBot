using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class MilestoneService : INService, IReadyExecutor
{
    private static readonly int[] _milestones =
        [100, 250, 500, 1_000, 2_500, 5_000, 10_000, 25_000, 50_000, 100_000];

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly NonBlocking.ConcurrentDictionary<ulong, MilestoneConfig> _configs = new();

    public MilestoneService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var all = await ctx.GetTable<MilestoneConfig>().ToListAsyncLinqToDB();
        foreach (var c in all)
            _configs[c.GuildId] = c;

        _client.UserJoined += OnUserJoined;
    }

    private Task OnUserJoined(SocketGuildUser user)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!_configs.TryGetValue(user.Guild.Id, out var config) || !config.Enabled || config.ChannelId is null)
                    return;

                var memberCount = user.Guild.MemberCount;
                var nextMilestone = GetNextMilestone(config.LastMilestone);

                if (nextMilestone is null || memberCount < nextMilestone.Value)
                    return;

                // We hit or passed a milestone
                config.LastMilestone = nextMilestone.Value;

                await using var ctx = _db.GetDbContext();
                await ctx.GetTable<MilestoneConfig>()
                    .Where(x => x.GuildId == user.Guild.Id)
                    .Set(x => x.LastMilestone, nextMilestone.Value)
                    .UpdateAsync();

                var channel = user.Guild.GetTextChannel(config.ChannelId.Value);
                if (channel is null)
                    return;

                var embed = _sender.CreateEmbed(user.Guild.Id)
                    .WithTitle("Server Milestone Reached!")
                    .WithDescription($"This server just hit **{nextMilestone.Value:N0}** members!")
                    .AddField("Current Members", memberCount.ToString("N0"), true)
                    .AddField("Next Milestone", GetNextMilestone(nextMilestone.Value)?.ToString("N0") ?? "No more milestones", true)
                    .WithOkColor();

                await _sender.Response(channel).Embed(embed).SendAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in milestone service for guild {GuildId}", user.Guild.Id);
            }
        });

        return Task.CompletedTask;
    }

    public static int? GetNextMilestone(int lastMilestone)
    {
        foreach (var m in _milestones)
        {
            if (m > lastMilestone)
                return m;
        }

        return null;
    }

    public async Task SetChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<MilestoneConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            var config = new MilestoneConfig
            {
                GuildId = guildId,
                ChannelId = channelId,
                Enabled = true,
                LastMilestone = 0
            };

            await ctx.GetTable<MilestoneConfig>().InsertAsync(() => new MilestoneConfig
            {
                GuildId = guildId,
                ChannelId = channelId,
                Enabled = true,
                LastMilestone = 0
            });

            _configs[guildId] = config;
        }
        else
        {
            await ctx.GetTable<MilestoneConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.ChannelId, channelId)
                .Set(x => x.Enabled, true)
                .UpdateAsync();

            existing.ChannelId = channelId;
            existing.Enabled = true;
            _configs[guildId] = existing;
        }
    }

    public async Task DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<MilestoneConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.Enabled, false)
            .UpdateAsync();

        if (_configs.TryGetValue(guildId, out var config))
            config.Enabled = false;
    }

    public MilestoneConfig? GetConfig(ulong guildId)
        => _configs.TryGetValue(guildId, out var c) ? c : null;
}
