#nullable disable
using SantiBot.Modules.Utility.Developer;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Developer")]
    [Group("dev")]
    public partial class DeveloperCommands : SantiModule<DeveloperService>
    {
        // ═══════════════════════════════════════════
        //  FEATURE FLAGS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task FeatureFlag(string featureName, bool? enabled = null)
        {
            if (enabled is null)
            {
                var isEnabled = await _service.IsFlagEnabledAsync(ctx.Guild.Id, featureName);
                await Response().Confirm($"🚩 Feature `{featureName}` is **{(isEnabled ? "ENABLED" : "DISABLED")}**").SendAsync();
                return;
            }

            await _service.SetFlagAsync(ctx.Guild.Id, featureName, enabled.Value);
            await Response().Confirm($"🚩 Feature `{featureName}` is now **{(enabled.Value ? "ENABLED" : "DISABLED")}**").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task FeatureFlags()
        {
            var flags = await _service.GetAllFlagsAsync(ctx.Guild.Id);
            if (flags.Count == 0)
            {
                await Response().Confirm("No feature flags set. Use `.dev featureflag <name> true/false` to create one.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var f in flags)
            {
                var status = f.IsEnabled ? "🟢" : "🔴";
                sb.AppendLine($"{status} `{f.FeatureName}` — {f.RolloutPercent}% rollout");
            }

            var eb = CreateEmbed()
                .WithTitle($"🚩 Feature Flags ({flags.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  WEBHOOKS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task WebhookCreate(string name, ITextChannel channel, string eventType = "message")
        {
            var webhook = await _service.CreateWebhookAsync(ctx.Guild.Id, name, channel.Id.ToString(), eventType);
            await Response().Confirm($"🔗 Webhook **{name}** created!\n" +
                $"Target: {channel.Mention}\n" +
                $"Event: `{eventType}`\n" +
                $"Secret: `{webhook.Secret}`\n" +
                $"*Keep the secret safe — it's used to authenticate webhook calls.*").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Webhooks()
        {
            var webhooks = await _service.GetWebhooksAsync(ctx.Guild.Id);
            if (webhooks.Count == 0)
            {
                await Response().Confirm("No webhooks configured.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var w in webhooks)
            {
                var status = w.IsEnabled ? "🟢" : "🔴";
                sb.AppendLine($"{status} **{w.Name}** (ID: {w.Id}) — `{w.EventType}` → <#{w.TargetChannelId}> ({w.TriggerCount} triggers)");
            }

            var eb = CreateEmbed()
                .WithTitle($"🔗 Webhook Endpoints ({webhooks.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task WebhookDelete(int webhookId)
        {
            var success = await _service.DeleteWebhookAsync(ctx.Guild.Id, webhookId);
            if (success) await Response().Confirm("Webhook deleted!").SendAsync();
            else await Response().Error("Webhook not found!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  COMMAND ANALYTICS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task CmdStats(int days = 7)
        {
            var (total, success, failed, avgMs) = await _service.GetCommandStatsAsync(ctx.Guild.Id, days);
            var topCmds = await _service.GetTopCommandsAsync(ctx.Guild.Id, days);

            var sb = new System.Text.StringBuilder();
            if (topCmds.Count > 0)
            {
                sb.AppendLine("**Top Commands:**");
                var rank = 1;
                foreach (var (cmd, count) in topCmds.Take(10))
                {
                    sb.AppendLine($"  {rank}. `.{cmd}` — {count:N0} uses");
                    rank++;
                }
            }

            var eb = CreateEmbed()
                .WithTitle($"📊 Command Analytics (Last {days} days)")
                .AddField("Total Commands", $"**{total:N0}**", true)
                .AddField("Success Rate", total > 0 ? $"**{success * 100 / total}%**" : "N/A", true)
                .AddField("Avg Response", $"**{avgMs:F0}ms**", true)
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  XP MULTIPLIERS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpMult(double multiplier, int durationHours = 0)
        {
            var expires = durationHours > 0 ? DateTime.UtcNow.AddHours(durationHours) : (DateTime?)null;
            await _service.SetMultiplierAsync(ctx.Guild.Id, "Global", 0, multiplier, expires);
            var durStr = durationHours > 0 ? $" for {durationHours} hours" : " (permanent)";
            await Response().Confirm($"⭐ Global XP multiplier set to **{multiplier}x**{durStr}!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpMultChannel(ITextChannel channel, double multiplier)
        {
            await _service.SetMultiplierAsync(ctx.Guild.Id, "Channel", channel.Id, multiplier, null);
            await Response().Confirm($"⭐ {channel.Mention} XP multiplier set to **{multiplier}x**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpMultRole(IRole role, double multiplier)
        {
            await _service.SetMultiplierAsync(ctx.Guild.Id, "Role", role.Id, multiplier, null);
            await Response().Confirm($"⭐ {role.Mention} XP multiplier set to **{multiplier}x**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task XpMults()
        {
            var mults = await _service.GetMultipliersAsync(ctx.Guild.Id);
            if (mults.Count == 0)
            {
                await Response().Confirm("No XP multipliers active.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var m in mults)
            {
                var target = m.Type switch
                {
                    "Global" or "Event" => "Server-wide",
                    "Channel" => $"<#{m.TargetId}>",
                    "Role" => $"<@&{m.TargetId}>",
                    _ => "Unknown",
                };
                var expires = m.ExpiresAt.HasValue ? $" (expires <t:{new DateTimeOffset(m.ExpiresAt.Value).ToUnixTimeSeconds()}:R>)" : "";
                sb.AppendLine($"⭐ **{m.Multiplier}x** — {m.Type}: {target}{expires}");
            }

            var eb = CreateEmbed()
                .WithTitle("⭐ Active XP Multipliers")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  XP CHALLENGES
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpChallenge(string name, string requirement, long xpReward, int durationDays = 7, [Leftover] string description = null)
        {
            description ??= name;
            var challenge = await _service.CreateChallengeAsync(ctx.Guild.Id, name, description, requirement, xpReward, durationDays);
            await Response().Confirm($"🎯 XP Challenge **{name}** created!\n" +
                $"Requirement: `{requirement}`\n" +
                $"Reward: **{xpReward}** XP\n" +
                $"Duration: **{durationDays}** days").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task XpChallenges()
        {
            var challenges = await _service.GetActiveChallengesAsync(ctx.Guild.Id);
            if (challenges.Count == 0)
            {
                await Response().Confirm("No active XP challenges!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var c in challenges)
            {
                var timeLeft = c.EndsAt - DateTime.UtcNow;
                sb.AppendLine($"🎯 **{c.ChallengeName}** — {c.Description}");
                sb.AppendLine($"  Requirement: `{c.Requirement}` | Reward: **{c.XpReward}** XP | Ends in {(int)timeLeft.TotalDays}d\n");
            }

            var eb = CreateEmbed()
                .WithTitle($"🎯 Active XP Challenges ({challenges.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  CHANGELOG
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Changelog()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (version, date, changes) in DeveloperService.Changelog)
                sb.AppendLine($"**v{version}** ({date})\n  {changes}\n");

            var eb = CreateEmbed()
                .WithTitle("📋 SantiBot Changelog")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  BOT INFO
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BotInfo()
        {
            var guilds = ((DiscordSocketClient)ctx.Client).Guilds.Count;
            var users = ((DiscordSocketClient)ctx.Client).Guilds.Sum(g => g.MemberCount);
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

            var eb = CreateEmbed()
                .WithTitle("🤖 SantiBot Info")
                .AddField("Version", "1.0.0", true)
                .AddField("Servers", $"{guilds:N0}", true)
                .AddField("Users", $"{users:N0}", true)
                .AddField("Uptime", $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m", true)
                .AddField("Features", "1000+", true)
                .AddField("Commands", "800+", true)
                .WithFooter("Built with love by Snuggs & Claude")
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }
    }
}
