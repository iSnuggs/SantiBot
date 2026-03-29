#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class ReactionRolesV2Service : INService, IReadyExecutor
{
    private const string DD_PREFIX = "n:ddrole:";

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public ReactionRolesV2Service(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.InteractionCreated += OnInteraction;
        return Task.CompletedTask;
    }

    private async Task OnInteraction(SocketInteraction inter)
    {
        if (inter is not SocketMessageComponent smc)
            return;

        if (!smc.Data.CustomId.StartsWith(DD_PREFIX))
            return;

        await inter.DeferAsync(true);

        _ = Task.Run(async () =>
        {
            try
            {
                if (smc.Data.Values is null || !smc.Data.Values.Any())
                    return;

                await using var ctx = _db.GetDbContext();

                // Get all options for this message
                var options = await ctx.GetTable<DropdownRoleOption>()
                    .Where(x => x.MessageId == smc.Message.Id)
                    .ToListAsyncLinqToDB();

                if (options.Count == 0) return;

                var guild = _client.GetGuild(options[0].GuildId);
                if (guild is null) return;

                var guildUser = guild.GetUser(smc.User.Id);
                if (guildUser is null) return;

                var selectedLabels = smc.Data.Values.ToHashSet();

                foreach (var opt in options)
                {
                    var role = guild.GetRole(opt.RoleId);
                    if (role is null) continue;

                    if (selectedLabels.Contains(opt.Label))
                    {
                        if (!guildUser.Roles.Any(r => r.Id == opt.RoleId))
                            await guildUser.AddRoleAsync(role);
                    }
                    else
                    {
                        if (guildUser.Roles.Any(r => r.Id == opt.RoleId))
                            await guildUser.RemoveRoleAsync(role);
                    }
                }

                await smc.FollowupAsync("Roles updated!", ephemeral: true);
            }
            catch (Exception ex)
            {
                await smc.FollowupAsync($"Error: {ex.Message}", ephemeral: true);
            }
        });
    }

    public async Task<DropdownRolePanel> CreatePanelAsync(ulong guildId, ulong channelId, string title)
    {
        await using var ctx = _db.GetDbContext();
        var id = await ctx.GetTable<DropdownRolePanel>()
            .InsertWithInt32IdentityAsync(() => new DropdownRolePanel
            {
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = 0, // Will be set after message is sent
                Title = title
            });

        return new DropdownRolePanel { Id = id, GuildId = guildId, ChannelId = channelId, Title = title };
    }

    public async Task UpdatePanelMessageIdAsync(int panelId, ulong messageId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<DropdownRolePanel>()
            .Where(x => x.Id == panelId)
            .UpdateAsync(x => new DropdownRolePanel { MessageId = messageId });
    }

    public async Task<DropdownRoleOption> AddOptionAsync(ulong guildId, ulong messageId, string label, ulong roleId, string emote = null, string description = null)
    {
        await using var ctx = _db.GetDbContext();

        var panel = await ctx.GetTable<DropdownRolePanel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.MessageId == messageId);

        if (panel is null) return null;

        var id = await ctx.GetTable<DropdownRoleOption>()
            .InsertWithInt32IdentityAsync(() => new DropdownRoleOption
            {
                PanelId = panel.Id,
                GuildId = guildId,
                MessageId = messageId,
                Label = label,
                RoleId = roleId,
                Emote = emote ?? "",
                Description = description ?? ""
            });

        return new DropdownRoleOption { Id = id, Label = label, RoleId = roleId };
    }

    public async Task<List<DropdownRoleOption>> GetOptionsAsync(ulong messageId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<DropdownRoleOption>()
            .Where(x => x.MessageId == messageId)
            .ToListAsyncLinqToDB();
    }

    public string GetCustomId() => DD_PREFIX + Guid.NewGuid().ToString("N")[..8];
}
