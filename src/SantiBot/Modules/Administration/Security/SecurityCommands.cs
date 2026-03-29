#nullable disable
using SantiBot.Modules.Administration.Security;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Name("Security")]
    [Group("security")]
    public partial class SecurityCommands : SantiModule<SecurityService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task SecurityAudit()
        {
            var guild = (SocketGuild)ctx.Guild;
            var findings = _service.RunSecurityAudit(guild);

            var critical = findings.Where(f => f.Severity == SecurityService.FindingSeverity.Critical).ToList();
            var warnings = findings.Where(f => f.Severity == SecurityService.FindingSeverity.Warning).ToList();
            var info = findings.Where(f => f.Severity == SecurityService.FindingSeverity.Info).ToList();

            var desc = "";

            if (critical.Count > 0)
            {
                desc += "**CRITICAL**\n";
                foreach (var f in critical)
                    desc += $"  {f.Description}\n";
                desc += "\n";
            }

            if (warnings.Count > 0)
            {
                desc += "**WARNING**\n";
                foreach (var f in warnings)
                    desc += $"  {f.Description}\n";
                desc += "\n";
            }

            if (info.Count > 0)
            {
                desc += "**INFO**\n";
                foreach (var f in info)
                    desc += $"  {f.Description}\n";
            }

            // Truncate if too long for embed
            if (desc.Length > 4000)
                desc = desc[..3997] + "...";

            var eb = CreateEmbed()
                .WithTitle("Security Audit Report")
                .WithDescription(desc)
                .AddField("Summary",
                    $"{critical.Count} Critical | {warnings.Count} Warnings | {info.Count} Info")
                .WithColor(critical.Count > 0 ? Color.Red : warnings.Count > 0 ? Color.Gold : Color.Green);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task SetHoneypot(ITextChannel channel)
        {
            _service.SetHoneypot(ctx.Guild.Id, channel.Id);
            await Response().Confirm($"Channel {channel.Mention} is now a honeypot. Any user who posts there will be flagged.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task RemoveHoneypot(ITextChannel channel)
        {
            if (_service.RemoveHoneypot(ctx.Guild.Id, channel.Id))
                await Response().Confirm($"Channel {channel.Mention} is no longer a honeypot.").SendAsync();
            else
                await Response().Error("That channel is not a honeypot.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task HoneypotList()
        {
            var channels = _service.GetHoneypotChannels(ctx.Guild.Id);

            if (channels.Count == 0)
            {
                await Response().Error("No honeypot channels configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", channels.Select(id =>
            {
                var ch = ctx.Guild.GetTextChannelAsync(id).GetAwaiter().GetResult();
                return ch is not null ? $"- {ch.Mention}" : $"- Unknown channel ({id})";
            }));

            var eb = CreateEmbed()
                .WithTitle("Honeypot Channels")
                .WithDescription(desc)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AccountGate(int days)
        {
            if (days < 0 || days > 365)
            {
                await Response().Error("Days must be between 0 and 365. Use 0 to disable.").SendAsync();
                return;
            }

            _service.SetAccountAgeGate(ctx.Guild.Id, days);

            if (days == 0)
                await Response().Confirm("Account age gate **disabled**.").SendAsync();
            else
                await Response().Confirm($"Account age gate set to **{days} days**. Accounts younger than that will be flagged.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task LeakScan()
        {
            var channel = (ITextChannel)ctx.Channel;
            var messages = (await channel.GetMessagesAsync(100).FlattenAsync()).ToList();

            var totalLeaks = 0;
            var leakDetails = new List<string>();

            foreach (var msg in messages)
            {
                if (string.IsNullOrWhiteSpace(msg.Content))
                    continue;

                var leaks = _service.DetectLeaks(msg.Content);
                var pii = _service.DetectPersonalInfo(msg.Content);

                if (leaks.Count > 0 || pii.Count > 0)
                {
                    var allFindings = leaks.Concat(pii).ToList();
                    totalLeaks += allFindings.Count;
                    leakDetails.Add($"**{msg.Author.Username}** ({msg.Timestamp:g}): {string.Join(", ", allFindings)}");

                    if (leakDetails.Count >= 20)
                        break;
                }
            }

            if (totalLeaks == 0)
            {
                var eb = CreateEmbed()
                    .WithTitle("Leak Scan Complete")
                    .WithDescription("No tokens, API keys, or personal information detected in the last 100 messages.")
                    .WithColor(Color.Green);

                await Response().Embed(eb).SendAsync();
                return;
            }

            var desc = string.Join("\n", leakDetails);
            if (desc.Length > 4000)
                desc = desc[..3997] + "...";

            var embed = CreateEmbed()
                .WithTitle("Leak Scan Results")
                .WithDescription(desc)
                .AddField("Total Findings", totalLeaks.ToString(), true)
                .AddField("Messages Scanned", messages.Count.ToString(), true)
                .WithColor(Color.Red);

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ScamScan()
        {
            var channel = (ITextChannel)ctx.Channel;
            var messages = (await channel.GetMessagesAsync(100).FlattenAsync()).ToList();

            var scamMessages = new List<string>();

            foreach (var msg in messages)
            {
                if (string.IsNullOrWhiteSpace(msg.Content))
                    continue;

                var result = _service.DetectScam(msg.Content);
                if (result.IsSuspicious)
                {
                    scamMessages.Add($"**{msg.Author.Username}** ({msg.Timestamp:g}): {result.Reason}");

                    if (scamMessages.Count >= 20)
                        break;
                }
            }

            if (scamMessages.Count == 0)
            {
                var eb = CreateEmbed()
                    .WithTitle("Scam Scan Complete")
                    .WithDescription("No scam patterns detected in the last 100 messages.")
                    .WithColor(Color.Green);

                await Response().Embed(eb).SendAsync();
                return;
            }

            var desc = string.Join("\n", scamMessages);
            if (desc.Length > 4000)
                desc = desc[..3997] + "...";

            var embed = CreateEmbed()
                .WithTitle("Scam Scan Results")
                .WithDescription(desc)
                .AddField("Suspicious Messages", scamMessages.Count.ToString(), true)
                .AddField("Messages Scanned", messages.Count.ToString(), true)
                .WithColor(Color.Red);

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SecurityScan()
        {
            var guild = (SocketGuild)ctx.Guild;

            // Quick audit
            var findings = _service.RunSecurityAudit(guild);
            var criticalCount = findings.Count(f => f.Severity == SecurityService.FindingSeverity.Critical);
            var warningCount = findings.Count(f => f.Severity == SecurityService.FindingSeverity.Warning);

            // Account gate status
            var ageDays = _service.GetAccountAgeGate(ctx.Guild.Id);

            // Honeypot status
            var honeypots = _service.GetHoneypotChannels(ctx.Guild.Id);
            var caughtUsers = _service.GetCaughtUsers(ctx.Guild.Id);

            var eb = CreateEmbed()
                .WithTitle("Security Status Report")
                .AddField("Audit Findings",
                    $"{criticalCount} Critical\n{warningCount} Warnings\n{findings.Count} Total",
                    true)
                .AddField("Account Age Gate",
                    ageDays > 0 ? $"{ageDays} day minimum" : "Disabled",
                    true)
                .AddField("Honeypots",
                    $"{honeypots.Count} channels active\n{caughtUsers.Count} users caught",
                    true)
                .AddField("Server Info",
                    $"Members: {guild.MemberCount}\n"
                    + $"Roles: {guild.Roles.Count}\n"
                    + $"Text Channels: {guild.TextChannels.Count}\n"
                    + $"Bots: {guild.Users.Count(u => u.IsBot)}",
                    true)
                .WithColor(criticalCount > 0 ? Color.Red : warningCount > 0 ? Color.Gold : Color.Green);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task SecurityReport()
        {
            var guild = (SocketGuild)ctx.Guild;

            // Full audit
            var findings = _service.RunSecurityAudit(guild);
            var critical = findings.Where(f => f.Severity == SecurityService.FindingSeverity.Critical).ToList();
            var warnings = findings.Where(f => f.Severity == SecurityService.FindingSeverity.Warning).ToList();

            // Account gate
            var ageDays = _service.GetAccountAgeGate(ctx.Guild.Id);

            // Honeypots
            var honeypots = _service.GetHoneypotChannels(ctx.Guild.Id);
            var caughtUsers = _service.GetCaughtUsers(ctx.Guild.Id);

            // Scan current channel for leaks and scams
            var channel = (ITextChannel)ctx.Channel;
            var messages = (await channel.GetMessagesAsync(100).FlattenAsync()).ToList();

            var leakCount = 0;
            var scamCount = 0;

            foreach (var msg in messages)
            {
                if (string.IsNullOrWhiteSpace(msg.Content))
                    continue;

                var leaks = _service.DetectLeaks(msg.Content);
                var pii = _service.DetectPersonalInfo(msg.Content);
                leakCount += leaks.Count + pii.Count;

                var scam = _service.DetectScam(msg.Content);
                if (scam.IsSuspicious)
                    scamCount++;
            }

            // Build comprehensive report
            var auditSummary = "";
            if (critical.Count > 0)
                auditSummary += $"**{critical.Count} CRITICAL issues found!**\n";
            if (warnings.Count > 0)
                auditSummary += $"{warnings.Count} warnings found\n";
            if (critical.Count == 0 && warnings.Count == 0)
                auditSummary += "No issues found";

            var eb = CreateEmbed()
                .WithTitle("Comprehensive Security Report")
                .AddField("Permission Audit", auditSummary, false)
                .AddField("Account Age Gate",
                    ageDays > 0 ? $"Active: {ageDays} day minimum" : "Not configured",
                    true)
                .AddField("Honeypot System",
                    $"{honeypots.Count} channel(s) active | {caughtUsers.Count} user(s) caught",
                    true)
                .AddField("Channel Scan (last 100 msgs)",
                    $"Leaked credentials/PII: {leakCount}\nScam messages: {scamCount}",
                    false)
                .AddField("Server Stats",
                    $"Members: {guild.MemberCount} | "
                    + $"Roles: {guild.Roles.Count} | "
                    + $"Bots: {guild.Users.Count(u => u.IsBot)} | "
                    + $"Channels: {guild.TextChannels.Count + guild.VoiceChannels.Count}",
                    false)
                .AddField("Recommendations",
                    GetRecommendations(critical.Count, warnings.Count, ageDays, honeypots.Count, leakCount, scamCount),
                    false)
                .WithColor(critical.Count > 0 || leakCount > 0
                    ? Color.Red
                    : warnings.Count > 0 || scamCount > 0
                        ? Color.Gold
                        : Color.Green);

            await Response().Embed(eb).SendAsync();
        }

        private static string GetRecommendations(int critical, int warnings, int ageDays, int honeypots, int leaks, int scams)
        {
            var recs = new List<string>();

            if (critical > 0)
                recs.Add("Review roles with Administrator permission immediately");
            if (warnings > 0)
                recs.Add("Audit roles with elevated permissions and restrict where possible");
            if (ageDays == 0)
                recs.Add("Consider enabling account age gate (`.security accountgate 7`)");
            if (honeypots == 0)
                recs.Add("Consider setting up a honeypot channel to catch bots/raiders");
            if (leaks > 0)
                recs.Add("Delete messages containing leaked credentials/personal info ASAP");
            if (scams > 0)
                recs.Add("Review and delete flagged scam messages");

            if (recs.Count == 0)
                recs.Add("Server security looks solid! Keep monitoring regularly.");

            return string.Join("\n", recs.Select(r => $"- {r}"));
        }
    }
}
