namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Group("gameserver")]
    [Name("GameServer")]
    public partial class GameServerCommands : SantiModule<GameServerService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GameServerAdd(string gameType, string address, int port, ITextChannel channel)
        {
            var watch = await _service.AddServerAsync(ctx.Guild.Id, channel.Id, gameType, address, port);
            await Response()
                .Confirm(strs.gs_added(gameType, address, port, channel.Mention))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GameServerRemove(int id)
        {
            var removed = await _service.RemoveServerAsync(ctx.Guild.Id, id);
            if (removed)
                await Response().Confirm(strs.gs_removed(id)).SendAsync();
            else
                await Response().Error(strs.gs_not_found(id)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GameServerList()
        {
            var servers = await _service.ListServersAsync(ctx.Guild.Id);
            if (servers.Count == 0)
            {
                await Response().Error(strs.gs_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Watched Game Servers");

            foreach (var s in servers)
            {
                eb.AddField($"[{s.Id}] {s.GameType.ToUpper()}",
                    $"`{s.ServerAddress}:{s.ServerPort}` | Channel: <#{s.ChannelId}> | Auto-update: {(s.AutoUpdate ? "Yes" : "No")}",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GameServerCheck(string gameType, string address, int port)
        {
            var msg = await Response().Confirm(strs.gs_checking).SendAsync();

            var status = await _service.QueryServerAsync(gameType, address, port);

            var eb = CreateEmbed()
                .WithTitle($"{gameType.ToUpper()} Server Status")
                .WithColor(status.IsOnline ? new Color(0x2ecc71) : new Color(0xe74c3c))
                .AddField("Server", $"`{address}:{port}`", true)
                .AddField("Status", status.IsOnline ? "Online" : "Offline", true)
                .WithCurrentTimestamp();

            if (status.IsOnline)
            {
                if (!string.IsNullOrEmpty(status.ServerName) && status.ServerName != address)
                    eb.AddField("Name", status.ServerName, false);

                if (status.PlayersOnline >= 0)
                    eb.AddField("Players", $"{status.PlayersOnline}/{status.MaxPlayers}", true);

                if (!string.IsNullOrEmpty(status.Version))
                    eb.AddField("Version", status.Version, true);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
