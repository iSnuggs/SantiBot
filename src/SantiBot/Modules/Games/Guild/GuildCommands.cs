#nullable disable
using System.Text;
using SantiBot.Modules.Games.Guild;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Guild")]
    [Group("guild")]
    public partial class GuildCommands : SantiModule<GuildService>
    {
        private readonly ICurrencyProvider _cp;

        public GuildCommands(ICurrencyProvider cp)
        {
            _cp = cp;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Create([Leftover] string input)
        {
            // Expected format: "Guild Name TAG" or "Guild Name TAG emoji"
            var parts = input.Split(' ');
            if (parts.Length < 2)
            {
                await Response().Error("Usage: `.guild create <name> <tag>` (tag must be 3-5 characters)").SendAsync();
                return;
            }

            var tag = parts[^1];
            string emoji = null;

            // Check if last part is an emoji and second-to-last is tag
            if (parts.Length >= 3 && tag.Length <= 2 && !char.IsLetterOrDigit(tag[0]))
            {
                emoji = tag;
                tag = parts[^2];
                var name = string.Join(' ', parts[..^2]);
                var (guild, error) = await _service.CreateGuildAsync(ctx.Guild.Id, ctx.User.Id, name, tag, emoji);
                if (guild is null)
                {
                    await Response().Error(error).SendAsync();
                    return;
                }

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle($"{guild.Emoji} Guild Created!")
                    .AddField("Name", guild.Name, true)
                    .AddField("Tag", $"[{guild.Tag}]", true)
                    .AddField("Leader", ctx.User.Mention, true)
                    .WithFooter("Use .guild recruit on/off to control recruitment");

                await Response().Embed(eb).SendAsync();
                return;
            }
            else
            {
                var name = string.Join(' ', parts[..^1]);
                var (guild, error) = await _service.CreateGuildAsync(ctx.Guild.Id, ctx.User.Id, name, tag, emoji);
                if (guild is null)
                {
                    await Response().Error(error).SendAsync();
                    return;
                }

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle($"{guild.Emoji} Guild Created!")
                    .AddField("Name", guild.Name, true)
                    .AddField("Tag", $"[{guild.Tag}]", true)
                    .AddField("Leader", ctx.User.Mention, true)
                    .WithFooter("Use .guild recruit on/off to control recruitment");

                await Response().Embed(eb).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Info(string tag)
        {
            var guild = await _service.GetGuildAsync(ctx.Guild.Id, tag);
            if (guild is null)
            {
                await Response().Error($"No guild with tag `[{tag.ToUpperInvariant()}]` found.").SendAsync();
                return;
            }

            var xpForNext = GuildService.GetXpForNextLevel(guild.Level);
            var xpProgress = xpForNext > 0
                ? $"{guild.Xp:N0} / {xpForNext:N0}"
                : $"{guild.Xp:N0} (MAX)";

            var perk = GuildService.GetLevelPerk(guild.Level);
            var sign = _cp.GetCurrencySign();

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{guild.Emoji} [{guild.Tag}] {guild.Name}")
                .WithDescription(guild.Description ?? "No description set.")
                .AddField("Level", $"{guild.Level}/10", true)
                .AddField("XP", xpProgress, true)
                .AddField("Members", $"{guild.MemberCount}/{guild.MaxMembers}", true)
                .AddField("Treasury", $"{guild.Treasury:N0}{sign}", true)
                .AddField("Recruiting", guild.IsRecruiting ? "Yes" : "No", true)
                .AddField("Leader", $"<@{guild.LeaderId}>", true)
                .AddField("Current Perk", perk)
                .WithFooter($"Created {guild.CreatedAt:yyyy-MM-dd}");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Join(string tag)
        {
            var (guild, error) = await _service.JoinGuildAsync(ctx.Guild.Id, ctx.User.Id, tag);
            if (guild is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm(
                $"{ctx.User.Mention} joined **[{guild.Tag}] {guild.Name}**! ({guild.MemberCount}/{guild.MaxMembers} members)")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Leave()
        {
            var (guildName, error) = await _service.LeaveGuildAsync(ctx.Guild.Id, ctx.User.Id);
            if (guildName is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"{ctx.User.Mention} left **{guildName}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Members()
        {
            var (guild, member) = await _service.GetUserGuildAsync(ctx.Guild.Id, ctx.User.Id);
            if (guild is null)
            {
                await Response().Error("You are not in a guild.").SendAsync();
                return;
            }

            var members = await _service.GetGuildMembersAsync(guild.Id);
            var sign = _cp.GetCurrencySign();

            var sb = new StringBuilder();
            var rankEmoji = new Dictionary<string, string>
            {
                ["Leader"] = "👑",
                ["Officer"] = "⭐",
                ["Member"] = "🟢",
                ["Recruit"] = "🔵",
            };

            foreach (var m in members)
            {
                var emoji = rankEmoji.GetValueOrDefault(m.Rank, "⚪");
                sb.AppendLine($"{emoji} <@{m.UserId}> - **{m.Rank}** | Donated: {m.ContributedCurrency:N0}{sign}");
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{guild.Emoji} [{guild.Tag}] {guild.Name} - Members")
                .WithDescription(sb.ToString())
                .WithFooter($"{members.Count}/{guild.MaxMembers} members");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Contribute(long amount)
        {
            var sign = _cp.GetCurrencySign();
            var (newTreasury, error) = await _service.ContributeAsync(ctx.Guild.Id, ctx.User.Id, amount);
            if (error is not null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm(
                $"{ctx.User.Mention} donated **{amount:N0}**{sign} to the guild treasury!\nNew treasury balance: **{newTreasury:N0}**{sign}")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Promote(IGuildUser user)
        {
            var (newRank, error) = await _service.PromoteMemberAsync(ctx.Guild.Id, ctx.User.Id, user.Id);
            if (newRank is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"{user.Mention} has been promoted to **{newRank}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Demote(IGuildUser user)
        {
            var (newRank, error) = await _service.DemoteMemberAsync(ctx.Guild.Id, ctx.User.Id, user.Id);
            if (newRank is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"{user.Mention} has been demoted to **{newRank}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Kick(IGuildUser user)
        {
            var (kickedName, error) = await _service.KickMemberAsync(ctx.Guild.Id, ctx.User.Id, user.Id);
            if (kickedName is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"{user.Mention} has been kicked from the guild.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task War(string targetTag)
        {
            var (war, error) = await _service.DeclareWarAsync(ctx.Guild.Id, ctx.User.Id, targetTag);
            if (war is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var (_, attacker, defender, _) = await _service.GetWarStatusAsync(ctx.Guild.Id, ctx.User.Id);

            var eb = CreateEmbed()
                .WithErrorColor()
                .WithTitle("War Declared!")
                .WithDescription(
                    $"**[{attacker?.Tag}] {attacker?.Name}** has declared war on **[{defender?.Tag}] {defender?.Name}**!\n\n"
                    + "Guild members can use `.guild warbattle` to contribute points.\n"
                    + $"War ends <t:{new DateTimeOffset(war.EndsAt).ToUnixTimeSeconds()}:R>.")
                .AddField("Attacker Score", "0", true)
                .AddField("Defender Score", "0", true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WarBattle()
        {
            var (points, total, enemyTag, error) = await _service.WarBattleAsync(ctx.Guild.Id, ctx.User.Id);
            if (error is not null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm(
                $"{ctx.User.Mention} scored **{points}** war point(s) against **[{enemyTag}]**!\nYour guild's total score: **{total}**")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WarStatus()
        {
            var (war, attacker, defender, error) = await _service.GetWarStatusAsync(ctx.Guild.Id, ctx.User.Id);
            if (error is not null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var timeLeft = war.EndsAt - DateTime.UtcNow;
            var timeStr = timeLeft.TotalSeconds > 0
                ? $"<t:{new DateTimeOffset(war.EndsAt).ToUnixTimeSeconds()}:R>"
                : "**ENDED** - A leader can use `.guild warstatus` to claim rewards";

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Guild War Status")
                .AddField($"[{attacker?.Tag}] {attacker?.Name}", $"Score: **{war.AttackerScore}**", true)
                .AddField("VS", "---", true)
                .AddField($"[{defender?.Tag}] {defender?.Name}", $"Score: **{war.DefenderScore}**", true)
                .AddField("Ends", timeStr)
                .WithFooter("Use .guild warbattle to contribute points");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Leaderboard()
        {
            var guilds = await _service.GetGuildLeaderboardAsync(ctx.Guild.Id);
            if (guilds.Count == 0)
            {
                await Response().Error("No guilds exist on this server yet.").SendAsync();
                return;
            }

            var sign = _cp.GetCurrencySign();
            var sb = new StringBuilder();
            for (var i = 0; i < guilds.Count; i++)
            {
                var g = guilds[i];
                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => $"#{i + 1}",
                };
                sb.AppendLine(
                    $"{medal} **[{g.Tag}] {g.Name}** | Lv.{g.Level} | {g.MemberCount} members | {g.Treasury:N0}{sign} treasury");
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Guild Leaderboard")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Recruit(string onOff)
        {
            var recruiting = onOff.ToLowerInvariant() switch
            {
                "on" or "true" or "yes" => true,
                "off" or "false" or "no" => false,
                _ => (bool?)null,
            };

            if (recruiting is null)
            {
                await Response().Error("Usage: `.guild recruit on` or `.guild recruit off`").SendAsync();
                return;
            }

            var (newStatus, error) = await _service.SetRecruitingAsync(ctx.Guild.Id, ctx.User.Id, recruiting.Value);
            if (error is not null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm(
                $"Guild recruiting is now **{(newStatus ? "ON" : "OFF")}**.")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SetDescription([Leftover] string description)
        {
            var (guildName, error) = await _service.SetDescriptionAsync(ctx.Guild.Id, ctx.User.Id, description);
            if (error is not null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"Updated description for **{guildName}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Disband()
        {
            var (guildName, treasury, error) = await _service.DisbandGuildAsync(ctx.Guild.Id, ctx.User.Id);
            if (error is not null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var sign = _cp.GetCurrencySign();
            var msg = $"**{guildName}** has been disbanded.";
            if (treasury > 0)
                msg += $"\nTreasury of **{treasury:N0}**{sign} has been returned to you.";

            await Response().Confirm(msg).SendAsync();
        }
    }
}
