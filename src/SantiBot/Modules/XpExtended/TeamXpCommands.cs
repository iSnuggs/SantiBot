#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("Team")]
    [Group("team")]
    public partial class TeamXpCommands : SantiModule<TeamXpService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TeamCreate([Leftover] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 30)
            {
                await Response().Error("Team name must be 1-30 characters!").SendAsync();
                return;
            }

            var (success, error) = await _service.CreateTeamAsync(ctx.Guild.Id, ctx.User.Id, name);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"\U0001F3C1 Team **{name}** created! Others can join with `.team join {name}`.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TeamJoin([Leftover] string name)
        {
            var (success, error) = await _service.JoinTeamAsync(ctx.Guild.Id, ctx.User.Id, name);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"You joined team **{name}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TeamLeave()
        {
            var (success, error) = await _service.LeaveTeamAsync(ctx.Guild.Id, ctx.User.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm("You left your team.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TeamLeaderboard()
        {
            var teams = await _service.GetLeaderboardAsync(ctx.Guild.Id);

            if (teams.Count == 0)
            {
                await Response().Confirm("No teams yet! Create one with `.team create <name>`.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < teams.Count; i++)
            {
                var t = teams[i];
                sb.AppendLine($"**#{i + 1}** {t.Name} — {t.TotalXp:N0} XP");
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F3C6 Team XP Leaderboard")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TeamInfo([Leftover] string name = null)
        {
            SantiBot.Db.Models.XpTeam team;

            if (string.IsNullOrWhiteSpace(name))
            {
                team = await _service.GetUserTeamAsync(ctx.Guild.Id, ctx.User.Id);
                if (team is null)
                {
                    await Response().Error("You're not in a team! Join or create one.").SendAsync();
                    return;
                }
            }
            else
            {
                team = await _service.GetTeamByNameAsync(ctx.Guild.Id, name);
                if (team is null)
                {
                    await Response().Error("Team not found!").SendAsync();
                    return;
                }
            }

            var members = await _service.GetTeamMembersAsync(ctx.Guild.Id, team.Id);
            var owner = await ctx.Guild.GetUserAsync(team.OwnerId);

            var memberNames = new System.Text.StringBuilder();
            foreach (var m in members)
            {
                var user = await ctx.Guild.GetUserAsync(m.UserId);
                var uName = user?.ToString() ?? $"User {m.UserId}";
                var isOwner = m.UserId == team.OwnerId ? " \U0001F451" : "";
                memberNames.AppendLine($"\u2022 {uName}{isOwner}");
            }

            var eb = CreateEmbed()
                .WithTitle($"\U0001F3C1 Team: {team.Name}")
                .AddField("Total XP", team.TotalXp.ToString("N0"), true)
                .AddField("Members", members.Count.ToString(), true)
                .AddField("Owner", owner?.ToString() ?? $"User {team.OwnerId}", true)
                .AddField("Roster", memberNames.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
