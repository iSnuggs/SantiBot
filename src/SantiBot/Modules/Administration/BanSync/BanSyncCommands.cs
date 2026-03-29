#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("bansync")]
    public partial class BanSyncCommands : SantiModule<BanSyncService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task BanSyncAdd(ulong serverId)
        {
            if (serverId == ctx.Guild.Id)
            {
                await Response().Error("Cannot sync with yourself.").SendAsync();
                return;
            }

            if (await _service.AddLinkAsync(ctx.Guild.Id, serverId))
                await Response().Confirm($"Ban sync link added with server **{serverId}**.").SendAsync();
            else
                await Response().Error("Link already exists.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task BanSyncRemove(ulong serverId)
        {
            if (await _service.RemoveLinkAsync(ctx.Guild.Id, serverId))
                await Response().Confirm($"Ban sync link removed for server **{serverId}**.").SendAsync();
            else
                await Response().Error("Link not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task BanSyncList()
        {
            var links = await _service.ListLinksAsync(ctx.Guild.Id);
            if (links.Count == 0)
            {
                await Response().Error("No ban sync links configured.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Ban Sync Links")
                .WithDescription(string.Join("\n", links.Select(l => $"• Server ID: `{l.LinkedGuildId}`")))
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task BanSyncPush()
        {
            var embed = CreateEmbed()
                .WithDescription("⚠️ This will push ALL bans from this server to linked servers.\nType `yes` to confirm.")
                .WithErrorColor();

            await Response().Embed(embed).SendAsync();
            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            if (input?.ToUpperInvariant() != "YES")
            {
                await Response().Error("Ban push cancelled.").SendAsync();
                return;
            }

            var count = await _service.PushBansAsync(ctx.Guild.Id, ctx.Guild);
            await Response().Confirm($"Pushed **{count}** bans to linked servers.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task BanSyncPull()
        {
            var embed = CreateEmbed()
                .WithDescription("⚠️ This will pull ALL bans from linked servers into this server.\nType `yes` to confirm.")
                .WithErrorColor();

            await Response().Embed(embed).SendAsync();
            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            if (input?.ToUpperInvariant() != "YES")
            {
                await Response().Error("Ban pull cancelled.").SendAsync();
                return;
            }

            var count = await _service.PullBansAsync(ctx.Guild.Id, ctx.Guild);
            await Response().Confirm($"Pulled **{count}** bans from linked servers.").SendAsync();
        }
    }
}
