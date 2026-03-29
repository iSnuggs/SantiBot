#nullable disable
using SantiBot.Db.Models;
using SantiBot.Modules.Games.Expansion;
using System.Text;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Expansion")]
    [Group("ex")]
    public partial class ExpansionCommands(ICurrencyProvider _cp) : SantiModule<ExpansionService>
    {
        // ═══════════════════════════════════════════════════════════
        //  SKILL TREE
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SkillTree()
        {
            var tree = await _service.GetOrCreateSkillTreeAsync(ctx.User.Id, ctx.Guild.Id);
            var available = await _service.CalculateAvailableSkillPointsAsync(ctx.User.Id, ctx.Guild.Id);

            if (!ExpansionService.ClassSkills.TryGetValue(tree.Class, out var skills))
            {
                await Response().Error("Your class doesn't have a skill tree defined yet.").SendAsync();
                return;
            }

            var levels = new[] { tree.Skill1Level, tree.Skill2Level, tree.Skill3Level, tree.Skill4Level, tree.Skill5Level };

            var sb = new StringBuilder();
            sb.AppendLine($"**Class:** {tree.Class}");
            sb.AppendLine($"**Available Skill Points:** {available}");
            sb.AppendLine();

            for (var i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];
                var lvl = levels[i];
                var bar = new string('\u2588', lvl) + new string('\u2591', 5 - lvl);
                sb.AppendLine($"**{i + 1}. {skill.Name}** [{bar}] {lvl}/5");
                sb.AppendLine($"   *{skill.Description}*");
                sb.AppendLine($"   Effect: {skill.Effect}");
                sb.AppendLine();
            }

            sb.AppendLine("Use `.ex skillupgrade <1-5>` to upgrade a skill.");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\U0001f333 Skill Tree - {ctx.User.Username}")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SkillUpgrade(int skillNum)
        {
            var tree = await _service.GetOrCreateSkillTreeAsync(ctx.User.Id, ctx.Guild.Id);

            if (!ExpansionService.ClassSkills.TryGetValue(tree.Class, out var skills))
            {
                await Response().Error("Your class doesn't have a skill tree defined yet.").SendAsync();
                return;
            }

            if (skillNum < 1 || skillNum > 5)
            {
                await Response().Error("Skill number must be between 1 and 5.").SendAsync();
                return;
            }

            var (success, error) = await _service.UpgradeSkillAsync(ctx.User.Id, ctx.Guild.Id, skillNum);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var skill = skills[skillNum - 1];
            var updatedTree = await _service.GetOrCreateSkillTreeAsync(ctx.User.Id, ctx.Guild.Id);
            var newLevel = skillNum switch
            {
                1 => updatedTree.Skill1Level,
                2 => updatedTree.Skill2Level,
                3 => updatedTree.Skill3Level,
                4 => updatedTree.Skill4Level,
                5 => updatedTree.Skill5Level,
                _ => 0,
            };

            var bar = new string('\u2588', newLevel) + new string('\u2591', 5 - newLevel);

            await Response().Confirm(
                $"\u2b06\ufe0f **{skill.Name}** upgraded to level {newLevel}! [{bar}]\n" +
                $"*{skill.Effect}*"
            ).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  PRESTIGE
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Prestige()
        {
            var (success, error, newPrestige) = await _service.PrestigeAsync(ctx.User.Id, ctx.Guild.Id);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var bonus = newPrestige * 5;

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\u2728 PRESTIGE ACHIEVED! \u2728")
                .WithDescription(
                    $"{ctx.User.Mention} has reached **Prestige {newPrestige}**!\n\n" +
                    $"Your dungeon level has been reset to 1.\n" +
                    $"Your skills have been reset.\n\n" +
                    $"**Permanent Bonus:** +{bonus}% to all stats!\n" +
                    $"Prestiges remaining: {20 - newPrestige}")
                .WithFooter($"Prestige {newPrestige}/20 | +{bonus}% stats");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PrestigeInfo()
        {
            var prestige = await _service.GetPrestigeAsync(ctx.User.Id, ctx.Guild.Id);

            var level = prestige?.PrestigeLevel ?? 0;
            var bonus = prestige?.PrestigeBonusPercent ?? 0;
            var lastPrestige = prestige?.LastPrestigeAt;

            var sb = new StringBuilder();
            sb.AppendLine($"**Prestige Level:** {level}/20");
            sb.AppendLine($"**Stat Bonus:** +{bonus}%");

            if (lastPrestige.HasValue)
                sb.AppendLine($"**Last Prestige:** {lastPrestige.Value:MMM dd, yyyy}");

            sb.AppendLine();
            sb.AppendLine("**How Prestige Works:**");
            sb.AppendLine("- Reach dungeon level 50+ to prestige");
            sb.AppendLine("- Resets your level to 1 and skills to 0");
            sb.AppendLine("- Gain +5% permanent stat bonus per prestige");
            sb.AppendLine("- Maximum 20 prestiges (+100% total bonus)");
            sb.AppendLine();

            // Progress bar to max prestige
            var filled = level;
            var empty = 20 - level;
            var progressBar = new string('\u2588', filled) + new string('\u2591', empty);
            sb.AppendLine($"Progress: [{progressBar}] {level}/20");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\u2b50 Prestige Info - {ctx.User.Username}")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  DUNGEON MODIFIERS
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Modifiers()
        {
            var active = await _service.GetActiveModifiersAsync(ctx.Guild.Id);

            if (active.Count == 0)
            {
                // Roll new ones if none active
                active = await _service.RollNewModifiersAsync(ctx.Guild.Id);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Currently active dungeon modifiers:\n");

            foreach (var mod in active)
            {
                sb.AppendLine($"\u26a1 **{mod.ModifierName}**");
                sb.AppendLine($"   {mod.Description}");
                sb.AppendLine($"   ATK: {mod.AtkMult:F1}x | DEF: {mod.DefMult:F1}x | HP: {mod.HpMult:F1}x | XP: {mod.XpMult:F1}x | Loot: {mod.LootMult:F1}x");
                sb.AppendLine();
            }

            sb.AppendLine("*Modifiers change when new dungeons are started.*");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\U0001f3b2 Active Dungeon Modifiers")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  BOUNTY BOARD
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BountyPost(IGuildUser target, long amount, [Leftover] string reason = null)
        {
            var (success, error) = await _service.PostBountyAsync(
                ctx.Guild.Id, ctx.User.Id, target.Id, amount, reason);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var sign = _cp.GetCurrencySign();

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\U0001f4e2 Bounty Posted!")
                .WithDescription(
                    $"{ctx.User.Mention} posted a bounty on {target.Mention}!\n\n" +
                    $"**Reward:** {amount}{sign}\n" +
                    $"**Reason:** {reason ?? "No reason given."}\n\n" +
                    $"Use `.ex bountyclaim @user` to challenge and claim!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BountyBoard()
        {
            var bounties = await _service.GetOpenBountiesAsync(ctx.Guild.Id);

            if (bounties.Count == 0)
            {
                await Response().Confirm("The bounty board is empty. Post one with `.ex bountypost @user amount`!").SendAsync();
                return;
            }

            var sign = _cp.GetCurrencySign();
            var sb = new StringBuilder();

            for (var i = 0; i < bounties.Count && i < 15; i++)
            {
                var b = bounties[i];
                sb.AppendLine($"**{i + 1}.** <@{b.TargetUserId}> - **{b.Amount}**{sign}");
                sb.AppendLine($"   Posted by: <@{b.PostedBy}> | {b.Reason}");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\U0001f4cb Bounty Board")
                .WithDescription(sb.ToString())
                .WithFooter($"{bounties.Count} active bounties");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BountyClaim(IGuildUser target)
        {
            var (success, error, amount) = await _service.ClaimBountyAsync(
                ctx.Guild.Id, ctx.User.Id, target.Id);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var sign = _cp.GetCurrencySign();

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\U0001f3af Bounty Claimed!")
                .WithDescription(
                    $"{ctx.User.Mention} defeated {target.Mention} in PvP combat!\n\n" +
                    $"**Bounty Collected:** {amount}{sign}\n\n" +
                    $"The target has been removed from the bounty board.");

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  TREASURE HUNTS
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task TreasureHide(string word, long reward)
        {
            var (success, error) = await _service.HideTreasureAsync(
                ctx.Guild.Id, ctx.Channel.Id, word, reward);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var sign = _cp.GetCurrencySign();

            // Delete the admin's command message to hide the word
            try { await ctx.Message.DeleteAsync(); } catch { }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\U0001f3f4\u200d\u2620\ufe0f Treasure Hunt Activated!")
                .WithDescription(
                    $"A treasure worth **{reward}**{sign} has been hidden in this channel!\n\n" +
                    $"The first person to type the secret word wins!\n" +
                    $"Use `.ex treasurehint` for a clue.");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TreasureHint()
        {
            var hint = await _service.GetTreasureHintAsync(ctx.Guild.Id, ctx.Channel.Id);

            if (hint is null)
            {
                await Response().Error("No active treasure hunt in this channel.").SendAsync();
                return;
            }

            await Response().Confirm($"\U0001f50d {hint}").SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  HOROSCOPE
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Horoscope([Leftover] string sign = null)
        {
            // If no sign given, check if user has one set
            if (string.IsNullOrWhiteSpace(sign))
            {
                sign = await _service.GetUserSignAsync(ctx.User.Id, ctx.Guild.Id);
                if (sign is null)
                {
                    await Response().Error(
                        "You haven't set your zodiac sign yet!\n" +
                        "Use `.ex horoscopeset <sign>` to set it.\n\n" +
                        "Available signs: " + string.Join(", ", ExpansionService.ZodiacSigns)
                    ).SendAsync();
                    return;
                }
            }

            var reading = _service.GetDailyHoroscope(sign);
            if (reading is null)
            {
                await Response().Error(
                    "Invalid zodiac sign. Available: " + string.Join(", ", ExpansionService.ZodiacSigns)
                ).SendAsync();
                return;
            }

            string Stars(int r) => new string('\u2b50', r) + new string('\u2796', 5 - r);

            var sb = new StringBuilder();
            sb.AppendLine($"**Date:** {reading.Date:MMMM dd, yyyy}");
            sb.AppendLine();
            sb.AppendLine($"\u2764\ufe0f **Love** {Stars(reading.LoveRating)}");
            sb.AppendLine($"  {reading.Love}");
            sb.AppendLine();
            sb.AppendLine($"\U0001f4bc **Career** {Stars(reading.CareerRating)}");
            sb.AppendLine($"  {reading.Career}");
            sb.AppendLine();
            sb.AppendLine($"\U0001f49a **Health** {Stars(reading.HealthRating)}");
            sb.AppendLine($"  {reading.Health}");
            sb.AppendLine();
            sb.AppendLine($"\U0001f340 **Luck** {Stars(reading.LuckRating)}");
            sb.AppendLine($"  {reading.Luck}");
            sb.AppendLine();
            sb.AppendLine($"\u2694\ufe0f **Adventure** {Stars(reading.AdventureRating)}");
            sb.AppendLine($"  {reading.Adventure}");
            sb.AppendLine();
            sb.AppendLine($"**Overall:** {Stars(reading.OverallRating)}");

            var signEmoji = reading.Sign switch
            {
                "Aries" => "\u2648",
                "Taurus" => "\u2649",
                "Gemini" => "\u264a",
                "Cancer" => "\u264b",
                "Leo" => "\u264c",
                "Virgo" => "\u264d",
                "Libra" => "\u264e",
                "Scorpio" => "\u264f",
                "Sagittarius" => "\u2650",
                "Capricorn" => "\u2651",
                "Aquarius" => "\u2652",
                "Pisces" => "\u2653",
                _ => "\u2728",
            };

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{signEmoji} Daily Horoscope - {reading.Sign}")
                .WithDescription(sb.ToString())
                .WithFooter("Readings reset daily at midnight UTC");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task HoroscopeSet([Leftover] string sign)
        {
            if (string.IsNullOrWhiteSpace(sign))
            {
                await Response().Error(
                    "Please specify a zodiac sign.\nAvailable: " + string.Join(", ", ExpansionService.ZodiacSigns)
                ).SendAsync();
                return;
            }

            // Find matching sign (case insensitive, partial match)
            var match = ExpansionService.ZodiacSigns.FirstOrDefault(z =>
                z.StartsWith(sign, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                await Response().Error(
                    $"'{sign}' is not a valid zodiac sign.\nAvailable: " + string.Join(", ", ExpansionService.ZodiacSigns)
                ).SendAsync();
                return;
            }

            await _service.SetUserSignAsync(ctx.User.Id, ctx.Guild.Id, match);
            await Response().Confirm($"\u2728 Your zodiac sign has been set to **{match}**! Use `.ex horoscope` to get your daily reading.").SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  GOAL TRACKER
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GoalSet(string name, int target, [Leftover] string description = null)
        {
            var (success, error) = await _service.SetGoalAsync(
                ctx.User.Id, ctx.Guild.Id, name, target, description);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm(
                $"\U0001f3af Goal **{name}** created!\n" +
                $"Target: {target}\n" +
                $"Track progress with `.ex goalprogress {name} <amount>`"
            ).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GoalProgress(string name, int amount = 1)
        {
            var (success, error, goal) = await _service.UpdateGoalProgressAsync(
                ctx.User.Id, ctx.Guild.Id, name, amount);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var percent = (int)((double)goal.CurrentValue / goal.TargetValue * 100);
            var barLen = 20;
            var filled = (int)(barLen * ((double)goal.CurrentValue / goal.TargetValue));
            var progressBar = new string('\u2588', filled) + new string('\u2591', barLen - filled);

            var sb = new StringBuilder();
            sb.AppendLine($"**{goal.GoalName}**");
            sb.AppendLine($"[{progressBar}] {percent}%");
            sb.AppendLine($"{goal.CurrentValue}/{goal.TargetValue}");

            if (goal.IsComplete)
            {
                sb.AppendLine();
                sb.AppendLine("\u2705 **GOAL COMPLETE!** Congratulations!");
            }

            await Response().Confirm(sb.ToString()).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Goals()
        {
            var goals = await _service.GetGoalsAsync(ctx.User.Id, ctx.Guild.Id);

            if (goals.Count == 0)
            {
                await Response().Confirm("You don't have any goals yet. Create one with `.ex goalset <name> <target> [description]`").SendAsync();
                return;
            }

            var sb = new StringBuilder();

            foreach (var goal in goals)
            {
                var percent = goal.TargetValue > 0
                    ? (int)((double)goal.CurrentValue / goal.TargetValue * 100)
                    : 0;
                var barLen = 10;
                var filled = goal.TargetValue > 0
                    ? (int)(barLen * ((double)goal.CurrentValue / goal.TargetValue))
                    : 0;
                var progressBar = new string('\u2588', filled) + new string('\u2591', barLen - filled);
                var status = goal.IsComplete ? "\u2705" : "\u23f3";

                sb.AppendLine($"{status} **{goal.GoalName}** [{progressBar}] {goal.CurrentValue}/{goal.TargetValue} ({percent}%)");
                if (!string.IsNullOrWhiteSpace(goal.Description) && goal.Description != "No description.")
                    sb.AppendLine($"   *{goal.Description}*");
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\U0001f4cb Goals - {ctx.User.Username}")
                .WithDescription(sb.ToString())
                .WithFooter($"{goals.Count(g => g.IsComplete)}/{goals.Count} completed");

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  SERVER NEWSPAPER
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Newspaper()
        {
            // Try to get the latest newspaper first
            var latest = await _service.GetLatestNewspaperAsync(ctx.Guild.Id);

            // If none exists or it's older than 24 hours, generate a new one
            if (latest is null || (DateTime.UtcNow - latest.PublishedAt).TotalHours >= 24)
            {
                // Gather data for the newspaper
                var guild = (SocketGuild)ctx.Guild;
                var memberCount = guild.MemberCount;

                // We generate generic content since we don't have full message tracking
                var topPosters = "Check the XP leaderboard to see who's been most active!";
                var topEvents = "Use `.ex bountyboard` to see active bounties.\nUse `.ex modifiers` to see dungeon modifiers.";
                var newMembers = "Welcome to all who joined recently!";

                latest = await _service.GenerateNewspaperAsync(
                    ctx.Guild.Id, ctx.User.Id, topPosters, topEvents, newMembers, memberCount);
            }

            // Split content into chunks if needed (Discord embed limit)
            var content = latest.Content;
            if (content.Length > 4000)
                content = content[..4000] + "\n*[Truncated]*";

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\U0001f4f0 The Server Times - Edition #{latest.Edition}")
                .WithDescription(content)
                .WithFooter($"Published {latest.PublishedAt:MMM dd, yyyy HH:mm} UTC | Generated by <@{latest.GeneratedBy}>");

            await Response().Embed(eb).SendAsync();
        }
    }
}
