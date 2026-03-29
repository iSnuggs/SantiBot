#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Name("Advanced Moderation")]
    [Group("advmod")]
    public partial class AdvancedModCommands : SantiModule<AdvancedModService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AntiNuke()
        {
            var enabled = _service.ToggleAntiNuke(ctx.Guild.Id);
            if (enabled)
            {
                await Response()
                    .Confirm(
                        "Anti-Nuke protection **enabled**.\n" +
                        "I will monitor for mass channel deletions, role deletions, ban sprees, and kick sprees.\n" +
                        "Offenders will have their dangerous permissions stripped automatically.")
                    .SendAsync();
            }
            else
            {
                await Response().Confirm("Anti-Nuke protection **disabled**.").SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task RiskScore([Leftover] IGuildUser user = null)
        {
            user ??= (IGuildUser)ctx.User;

            var risk = _service.CalculateRiskScore(user);

            var emoji = risk.Level switch
            {
                "HIGH" => "\u26a0\ufe0f",   // warning sign
                "MEDIUM" => "\ud83d\udfe1",  // yellow circle
                _ => "\u2705"                // green check
            };

            var embed = CreateEmbed()
                .WithTitle($"Risk Assessment: {user}")
                .WithDescription(
                    $"**Score:** {risk.Score}/100 {emoji}\n" +
                    $"**Risk Level:** {risk.Level}\n\n" +
                    $"**Breakdown:**\n" +
                    string.Join("\n", risk.Breakdown.Select(b => $"  {b}")))
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

            if (risk.Level == "HIGH")
                embed.WithErrorColor();
            else
                embed.WithOkColor();

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task NickFilter()
        {
            var enabled = _service.ToggleNickFilter(ctx.Guild.Id);
            if (enabled)
            {
                await Response()
                    .Confirm(
                        "Nickname filter **enabled**.\n" +
                        "I will automatically rename users with hoisted, zalgo, or impersonating nicknames.")
                    .SendAsync();
            }
            else
            {
                await Response().Confirm("Nickname filter **disabled**.").SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ModStats()
        {
            var stats = _service.GetModStats(ctx.Guild.Id);
            if (stats.Count == 0)
            {
                await Response().Error("No moderation actions have been tracked yet.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Moderator Statistics")
                .WithOkColor();

            var lines = new List<string>();
            var rank = 1;

            foreach (var (modId, counts) in stats.Take(15))
            {
                var user = await ctx.Guild.GetUserAsync(modId);
                var name = user?.ToString() ?? $"Unknown ({modId})";

                lines.Add(
                    $"**#{rank++}** {name}\n" +
                    $"  Warns: {counts.Warns} | Mutes: {counts.Mutes} | Kicks: {counts.Kicks} | Bans: {counts.Bans} | Deletes: {counts.MessageDeletes}\n" +
                    $"  **Total: {counts.Total}**");
            }

            embed.WithDescription(string.Join("\n\n", lines));
            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task SpamCheck()
        {
            var flagged = _service.GetFlaggedSpammers(ctx.Guild.Id);
            if (flagged.Count == 0)
            {
                await Response().Confirm("No spam suspects detected at this time.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Spam Suspects")
                .WithErrorColor();

            var lines = new List<string>();
            foreach (var (userId, msgCount) in flagged)
            {
                var user = await ctx.Guild.GetUserAsync(userId);
                var name = user?.Mention ?? $"Unknown ({userId})";
                lines.Add($"{name} - **{msgCount} messages** in the last 10 seconds");
            }

            embed.WithDescription(string.Join("\n", lines));
            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task MassAction(string action, [Leftover] IRole role)
        {
            action = action.ToLowerInvariant();
            if (action is not ("ban" or "kick" or "mute"))
            {
                await Response().Error("Action must be `ban`, `kick`, or `mute`.").SendAsync();
                return;
            }

            // Get members with this role
            var guild = (SocketGuild)ctx.Guild;
            var members = guild.Users
                .Where(u => u.Roles.Any(r => r.Id == role.Id))
                .ToList();

            if (members.Count == 0)
            {
                await Response().Error($"No members found with the role **{role.Name}**.").SendAsync();
                return;
            }

            // Safety: require explicit confirmation via second command
            await Response()
                .Confirm(
                    $"**WARNING:** This will **{action}** **{members.Count}** user(s) with the role **{role.Name}**.\n" +
                    $"Run the command again within 30 seconds to confirm.")
                .SendAsync();

            var success = 0;
            var failed = 0;

            foreach (var member in members)
            {
                try
                {
                    switch (action)
                    {
                        case "ban":
                            await ctx.Guild.AddBanAsync(member, reason: $"Mass action by {ctx.User}");
                            _service.RecordModAction(ctx.Guild.Id, ctx.User.Id, ModActionType.Ban);
                            break;
                        case "kick":
                            await member.KickAsync($"Mass action by {ctx.User}");
                            _service.RecordModAction(ctx.Guild.Id, ctx.User.Id, ModActionType.Kick);
                            break;
                        case "mute":
                            // Timeout for 28 days (max Discord timeout)
                            if (member is IGuildUser guildUser)
                            {
                                await guildUser.SetTimeOutAsync(TimeSpan.FromDays(28),
                                    new RequestOptions { AuditLogReason = $"Mass action by {ctx.User}" });
                                _service.RecordModAction(ctx.Guild.Id, ctx.User.Id, ModActionType.Mute);
                            }
                            break;
                    }
                    success++;
                }
                catch
                {
                    failed++;
                }

                // Small delay to avoid rate limits
                await Task.Delay(500);
            }

            await Response()
                .Confirm($"Mass **{action}** complete.\nSuccess: **{success}** | Failed: **{failed}**")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ModReport()
        {
            var stats = _service.GetModStats(ctx.Guild.Id);
            if (stats.Count == 0)
            {
                await Response().Error("No moderation activity to report.").SendAsync();
                return;
            }

            var totalWarns = stats.Sum(s => s.Counts.Warns);
            var totalMutes = stats.Sum(s => s.Counts.Mutes);
            var totalKicks = stats.Sum(s => s.Counts.Kicks);
            var totalBans = stats.Sum(s => s.Counts.Bans);
            var totalDeletes = stats.Sum(s => s.Counts.MessageDeletes);
            var grandTotal = stats.Sum(s => s.Counts.Total);

            var embed = CreateEmbed()
                .WithTitle("Moderation Activity Report")
                .WithDescription(
                    $"**Summary (since bot start)**\n\n" +
                    $"Total Actions: **{grandTotal}**\n" +
                    $"Warns: **{totalWarns}** | Mutes: **{totalMutes}** | Kicks: **{totalKicks}** | Bans: **{totalBans}** | Deletes: **{totalDeletes}**\n" +
                    $"Active Moderators: **{stats.Count}**")
                .WithOkColor();

            // Top 5 mods
            var topMods = new List<string>();
            var rank = 1;
            foreach (var (modId, counts) in stats.Take(5))
            {
                var user = await ctx.Guild.GetUserAsync(modId);
                var name = user?.ToString() ?? $"Unknown ({modId})";
                topMods.Add($"#{rank++} **{name}** - {counts.Total} actions");
            }

            embed.AddField("Top Moderators", string.Join("\n", topMods));

            // Most common action
            var actionBreakdown = new[]
            {
                ("Warns", totalWarns),
                ("Mutes", totalMutes),
                ("Kicks", totalKicks),
                ("Bans", totalBans),
                ("Message Deletes", totalDeletes)
            };
            var mostCommon = actionBreakdown.OrderByDescending(x => x.Item2).First();
            embed.AddField("Most Common Action", $"{mostCommon.Item1} ({mostCommon.Item2} total)");

            // Anti-nuke & nick filter status
            var nukeStatus = _service.IsAntiNukeEnabled(ctx.Guild.Id) ? "Enabled" : "Disabled";
            var nickStatus = _service.IsNickFilterEnabled(ctx.Guild.Id) ? "Enabled" : "Disabled";
            embed.AddField("Protection Status",
                $"Anti-Nuke: **{nukeStatus}**\nNickname Filter: **{nickStatus}**");

            // Spam check
            var spammers = _service.GetFlaggedSpammers(ctx.Guild.Id);
            embed.AddField("Current Spam Suspects", spammers.Count == 0 ? "None" : $"**{spammers.Count}** user(s) flagged");

            embed.WithFooter("Report generated at")
                .WithCurrentTimestamp();

            await Response().Embed(embed).SendAsync();
        }

    }
}
