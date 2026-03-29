#nullable disable
using SantiBot.Modules.Games.Pvp;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("PvP")]
    [Group("pvp")]
    public partial class PvpCommands : SantiModule<PvpService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Duel(IUser target)
        {
            var (success, message, embed) = await _service.Duel(
                ctx.User.Id, ctx.User.Username,
                target.Id, target.Username,
                ctx.Guild.Id);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PvpStats(IUser target = null)
        {
            var user = target ?? ctx.User;
            var stats = await _service.GetPvpStats(user.Id, ctx.Guild.Id);

            var winRate = (stats.Wins + stats.Losses) > 0
                ? (stats.Wins * 100.0 / (stats.Wins + stats.Losses)).ToString("F1")
                : "0.0";

            var eb = CreateEmbed()
                .WithTitle($"PvP Stats — {user.Username}")
                .AddField("Elo Rating", $"**{stats.Elo}**", true)
                .AddField("Record", $"**{stats.Wins}**W / **{stats.Losses}**L / **{stats.Draws}**D", true)
                .AddField("Win Rate", $"**{winRate}%**", true)
                .AddField("Win Streak", $"Current: **{stats.WinStreak}** | Best: **{stats.BestWinStreak}**", false)
                .AddField("Damage Stats",
                    $"Dealt: **{stats.TotalDamageDealt:N0}** | Received: **{stats.TotalDamageReceived:N0}**", false)
                .WithOkColor();

            if (stats.LastDuelAt.HasValue)
                eb.WithFooter($"Last duel: {stats.LastDuelAt.Value:yyyy-MM-dd HH:mm} UTC");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PvpLeaderboard()
        {
            var leaders = await _service.GetLeaderboard(ctx.Guild.Id);

            if (leaders.Count == 0)
            {
                await Response().Error("No PvP stats yet! Challenge someone with `.pvp duel @user`").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < leaders.Count; i++)
            {
                var s = leaders[i];
                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => $"**#{i + 1}**",
                };
                sb.AppendLine($"{medal} <@{s.UserId}> — **{s.Elo}** Elo | {s.Wins}W/{s.Losses}L | Streak: {s.WinStreak}");
            }

            var eb = CreateEmbed()
                .WithTitle("PvP Leaderboard")
                .WithDescription(sb.ToString())
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task TournamentCreate(
            string format, int maxPlayers, long entryFee, [Leftover] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                await Response().Error(
                    "Usage: `.pvp tournamentcreate <format> <maxPlayers> <entryFee> <name>`\n" +
                    "Formats: SingleElimination, DoubleElimination, RoundRobin").SendAsync();
                return;
            }

            var (success, message) = await _service.CreateTournament(
                ctx.Guild.Id, ctx.Channel.Id, ctx.User.Id,
                name, format, maxPlayers, entryFee);

            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TournamentJoin()
        {
            var (success, message) = await _service.RegisterForTournament(
                ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task TournamentStart()
        {
            var (success, message, matchResults) = await _service.StartTournament(
                ctx.Guild.Id, ctx.User.Id);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            // Post the final results
            var eb = CreateEmbed()
                .WithTitle("Tournament Complete!")
                .WithDescription(message)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TournamentBracket()
        {
            var (success, message) = await _service.GetTournamentBracket(ctx.Guild.Id);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("Tournament Bracket")
                .WithDescription(message)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TournamentInfo()
        {
            var (success, message) = await _service.GetTournamentInfo(ctx.Guild.Id);

            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("Tournament Info")
                .WithDescription(message)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task RandomTeams(int teamCount, params IUser[] users)
        {
            if (users.Length < 2)
            {
                await Response().Error("Mention at least 2 users to split into teams.").SendAsync();
                return;
            }

            var userList = users.Select(u => (u.Id, u.Username)).ToList();
            var (success, message) = _service.RandomTeams(userList, teamCount);

            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BalancedTeams(int teamCount, params IUser[] users)
        {
            if (users.Length < 2)
            {
                await Response().Error("Mention at least 2 users to split into teams.").SendAsync();
                return;
            }

            var userList = users.Select(u => (u.Id, u.Username)).ToList();
            var (success, message) = await _service.BalancedTeams(userList, teamCount, ctx.Guild.Id);

            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
