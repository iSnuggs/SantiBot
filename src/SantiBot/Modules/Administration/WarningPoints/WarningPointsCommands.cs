#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("warnp")]
    public partial class WarningPointsCommands : SantiModule<WarningPointsService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task WarnPoints(IGuildUser user, string severity = "minor", [Leftover] string reason = null)
        {
            var (warning, totalPoints, autoAction) = await _service.AddWarningAsync(
                ctx.Guild.Id, user.Id, ctx.User.Id, severity, reason);

            var embed = CreateEmbed()
                .WithTitle("Warning Points Issued")
                .AddField("User", user.Mention, true)
                .AddField("Points", $"+{warning.Points} ({warning.Severity})", true)
                .AddField("Total", totalPoints.ToString(), true)
                .AddField("Reason", reason ?? "No reason provided")
                .WithOkColor();

            if (autoAction is not null)
            {
                embed.AddField("Auto-Action Triggered", $"**{autoAction}** (threshold reached at {totalPoints} points)");

                // Execute auto-action
                try
                {
                    switch (autoAction.ToLowerInvariant())
                    {
                        case "mute":
                            if (user is SocketGuildUser sgu)
                                await sgu.SetTimeOutAsync(TimeSpan.FromHours(1));
                            break;
                        case "kick":
                            await user.KickAsync($"Warning points threshold: {totalPoints}");
                            break;
                        case "ban":
                            await ctx.Guild.AddBanAsync(user, reason: $"Warning points threshold: {totalPoints}");
                            break;
                    }
                }
                catch { embed.AddField("Note", "Failed to execute auto-action (missing permissions?)"); }
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WarnPConfig(int threshold, [Leftover] string action)
        {
            var validActions = new[] { "warn", "mute", "kick", "ban" };
            if (!validActions.Contains(action.ToLowerInvariant()))
            {
                await Response().Error("Invalid action. Use: warn, mute, kick, ban").SendAsync();
                return;
            }

            await _service.SetThresholdAsync(ctx.Guild.Id, threshold, action.ToLowerInvariant());
            await Response().Confirm($"Warning point threshold set: **{threshold} points** → **{action}**").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task WarnPStatus(IGuildUser user)
        {
            var (warnings, totalPoints) = await _service.GetStatusAsync(ctx.Guild.Id, user.Id);

            var embed = CreateEmbed()
                .WithTitle($"Warning Points: {user}")
                .WithDescription($"**Total Points: {totalPoints}**")
                .WithOkColor();

            foreach (var w in warnings.Take(15))
            {
                embed.AddField($"+{w.Points} ({w.Severity}) - {w.DateAdded:g}",
                    w.Reason ?? "No reason");
            }

            if (warnings.Count > 15)
                embed.WithFooter($"...and {warnings.Count - 15} more");

            await Response().Embed(embed).SendAsync();
        }
    }
}
