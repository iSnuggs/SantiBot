#nullable disable
using SantiBot.Modules.Games.Gamification;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Gamification")]
    [Group("gf")]
    public partial class GamificationCommands : SantiModule<GamificationService>
    {
        // ═══════════════════════════════════════════
        //  BADGES
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Badges(IUser user = null)
        {
            user ??= ctx.User;
            var badges = await _service.GetBadgesAsync(user.Id, ctx.Guild.Id);

            if (badges.Count == 0)
            {
                await Response().Confirm($"{user.Username} hasn't earned any badges yet! Play games, chat, and explore to earn them.").SendAsync();
                return;
            }

            var grouped = badges.GroupBy(b => b.Category).OrderBy(g => g.Key);
            var sb = new System.Text.StringBuilder();
            foreach (var group in grouped)
            {
                sb.AppendLine($"**{group.Key}** ({group.Count()})");
                foreach (var b in group)
                {
                    var display = b.IsDisplayed ? " ⭐" : "";
                    sb.AppendLine($"  {GamificationService.RarityEmoji(b.Rarity)} {b.Emoji} {b.BadgeName}{display}");
                }
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle($"🎖️ Badges ({badges.Count}/{GamificationService.AllBadges.Length})")
                .WithDescription(sb.ToString())
                .WithFooter("Use .gf badgelist to see all available badges")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Badgelist([Leftover] string category = null)
        {
            var badges = await _service.GetBadgesAsync(ctx.User.Id, ctx.Guild.Id);
            var earnedIds = badges.Select(b => b.BadgeId).ToHashSet();

            var filtered = string.IsNullOrWhiteSpace(category)
                ? GamificationService.AllBadges
                : GamificationService.AllBadges.Where(b => b.Category.Contains(category, System.StringComparison.OrdinalIgnoreCase)).ToArray();

            if (filtered.Length == 0)
            {
                var cats = GamificationService.AllBadges.Select(b => b.Category).Distinct();
                await Response().Error($"Unknown category! Available: {string.Join(", ", cats.Select(c => $"**{c}**"))}").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            var grouped = filtered.GroupBy(b => b.Category);
            foreach (var group in grouped)
            {
                sb.AppendLine($"**{group.Key}:**");
                foreach (var b in group)
                {
                    var check = earnedIds.Contains(b.Id) ? "✅" : "⬜";
                    sb.AppendLine($"{check} {GamificationService.RarityEmoji(b.Rarity)} {b.Emoji} **{b.Name}** — {b.HowToEarn}");
                }
                sb.AppendLine();
            }

            var desc = sb.ToString();
            if (desc.Length > 4096)
            {
                var cats = GamificationService.AllBadges.Select(b => b.Category).Distinct();
                var footer = $"\n*...truncated. Use `.gf badgelist <category>` — categories: {string.Join(", ", cats.Select(c => $"`{c}`"))}*";
                desc = desc[..(4096 - footer.Length)] + footer;
            }

            var eb = CreateEmbed()
                .WithTitle($"🎖️ Badge Catalog ({earnedIds.Count}/{GamificationService.AllBadges.Length} earned)")
                .WithDescription(desc)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BadgeDisplay(string badgeId)
        {
            var success = await _service.ToggleBadgeDisplayAsync(ctx.User.Id, ctx.Guild.Id, badgeId);
            if (success)
                await Response().Confirm("Badge display toggled! Displayed badges show on your profile.").SendAsync();
            else
                await Response().Error("Badge not found! Check `.gf badges` for your badge IDs.").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  TITLES
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Titles(IUser user = null)
        {
            user ??= ctx.User;
            var titles = await _service.GetTitlesAsync(user.Id, ctx.Guild.Id);

            if (titles.Count == 0)
            {
                await Response().Confirm($"{user.Username} hasn't earned any titles yet!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var t in titles)
            {
                var active = t.IsActive ? " **[ACTIVE]**" : "";
                sb.AppendLine($"🏷️ **{t.TitleName}**{active}");
            }

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle($"🏷️ Titles ({titles.Count}/{GamificationService.AllTitles.Length})")
                .WithDescription(sb.ToString())
                .WithFooter("Use .gf titleequip <id> to set your active title")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Titlelist()
        {
            var titles = await _service.GetTitlesAsync(ctx.User.Id, ctx.Guild.Id);
            var earnedIds = titles.Select(t => t.TitleId).ToHashSet();

            var sb = new System.Text.StringBuilder();
            foreach (var t in GamificationService.AllTitles)
            {
                var check = earnedIds.Contains(t.Id) ? "✅" : "⬜";
                sb.AppendLine($"{check} 🏷️ **{t.Name}** — {t.Requirement}");
            }

            var eb = CreateEmbed()
                .WithTitle($"🏷️ All Titles ({earnedIds.Count}/{GamificationService.AllTitles.Length})")
                .WithDescription(sb.ToString())
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TitleEquip([Leftover] string titleId)
        {
            var success = await _service.EquipTitleAsync(ctx.User.Id, ctx.Guild.Id, titleId.Trim().ToLower());
            if (success)
                await Response().Confirm("Title equipped! It'll show on your profile.").SendAsync();
            else
                await Response().Error("Title not found! Check `.gf titles` for your titles.").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  BATTLE PASS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BattlePass()
        {
            var config = await _service.GetOrCreateConfigAsync(ctx.Guild.Id);
            var progress = await _service.GetOrCreatePassAsync(ctx.User.Id, ctx.Guild.Id);

            var xpInTier = progress.SeasonXp % config.XpPerTier;
            var pct = config.XpPerTier > 0 ? (int)(xpInTier * 100 / config.XpPerTier) : 0;
            var bar = new string('█', pct / 5) + new string('░', 20 - pct / 5);
            var premium = progress.IsPremium ? "⭐ PREMIUM" : "FREE";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{config.SeasonName}** ({premium})");
            sb.AppendLine($"Tier **{progress.CurrentTier}** / {config.MaxTier}");
            sb.AppendLine($"[{bar}] {xpInTier}/{config.XpPerTier} XP to next tier");
            sb.AppendLine($"Total Season XP: **{progress.SeasonXp:N0}**\n");

            // Show nearby tier rewards
            var startTier = Math.Max(1, progress.CurrentTier - 1);
            var endTier = Math.Min(config.MaxTier, progress.CurrentTier + 3);
            for (var i = startTier; i <= endTier; i++)
            {
                var reward = GamificationService.BattlePassTiers.FirstOrDefault(t => t.Tier == i);
                if (reward == default) continue;
                var marker = i == progress.CurrentTier ? "→ " : "  ";
                var check = i < progress.CurrentTier ? "✅" : i == progress.CurrentTier ? "🔶" : "⬜";
                sb.AppendLine($"{marker}{check} **Tier {i}:** {reward.FreeReward}");
                if (progress.IsPremium)
                    sb.AppendLine($"      ⭐ {reward.PremiumReward}");
            }

            var daysLeft = (config.SeasonEndsAt - DateTime.UtcNow).TotalDays;

            var eb = CreateEmbed()
                .WithTitle("🎖️ Battle Pass")
                .WithDescription(sb.ToString())
                .WithFooter($"Season ends in {(int)daysLeft} days | Earn XP from daily challenges, dungeons, games, and chat")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BattlePassRewards(int page = 1)
        {
            var progress = await _service.GetOrCreatePassAsync(ctx.User.Id, ctx.Guild.Id);
            page = Math.Clamp(page, 1, 5);
            var start = (page - 1) * 10 + 1;
            var end = page * 10;

            var sb = new System.Text.StringBuilder();
            for (var i = start; i <= end; i++)
            {
                var reward = GamificationService.BattlePassTiers.FirstOrDefault(t => t.Tier == i);
                if (reward == default) continue;
                var check = i <= progress.CurrentTier ? "✅" : "⬜";
                sb.AppendLine($"{check} **Tier {i}**");
                sb.AppendLine($"  Free: {reward.FreeReward}");
                sb.AppendLine($"  ⭐ Premium: {reward.PremiumReward}\n");
            }

            var eb = CreateEmbed()
                .WithTitle($"🎖️ Battle Pass Rewards (Page {page}/5)")
                .WithDescription(sb.ToString())
                .WithFooter("Use .gf battlepassrewards <page> to see more")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task DailyChallenges()
        {
            // Reset daily count if it's a new day
            await _service.CheckDailyResetAsync(ctx.User.Id, ctx.Guild.Id);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("**Today's Daily Challenges:**\n");

            // Generate 3 deterministic dailies based on date
            var seed = DateTime.UtcNow.DayOfYear + DateTime.UtcNow.Year * 1000;
            var rng = new System.Random(seed);
            var templates = GamificationService.DailyChallengeTemplates;
            var indices = Enumerable.Range(0, templates.Length).OrderBy(_ => rng.Next()).Take(3).ToArray();

            for (var i = 0; i < 3; i++)
            {
                var challenge = templates[indices[i]];
                sb.AppendLine($"**{i + 1}.** {challenge.Desc} — **+{challenge.XpReward} Battle Pass XP**");
            }

            sb.AppendLine("\n*Complete challenges to progress your Battle Pass!*");

            var eb = CreateEmbed()
                .WithTitle("📋 Daily Challenges")
                .WithDescription(sb.ToString())
                .WithFooter("Challenges reset at midnight UTC")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  OVERVIEW
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GfStats(IUser user = null)
        {
            user ??= ctx.User;
            var badges = await _service.GetBadgesAsync(user.Id, ctx.Guild.Id);
            var titles = await _service.GetTitlesAsync(user.Id, ctx.Guild.Id);
            var activeTitle = titles.FirstOrDefault(t => t.IsActive);
            var pass = await _service.GetOrCreatePassAsync(user.Id, ctx.Guild.Id);

            var displayBadges = badges.Where(b => b.IsDisplayed).Take(8);
            var badgeStr = displayBadges.Any()
                ? string.Join(" ", displayBadges.Select(b => b.Emoji))
                : "None displayed";

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle("🎮 Gamification Overview")
                .AddField("Active Title", activeTitle?.TitleName ?? "None", true)
                .AddField("Badges", $"{badges.Count}/{GamificationService.AllBadges.Length}", true)
                .AddField("Titles", $"{titles.Count}/{GamificationService.AllTitles.Length}", true)
                .AddField("Battle Pass", $"Tier {pass.CurrentTier} | {pass.SeasonXp:N0} XP", true)
                .AddField("Displayed Badges", badgeStr, false)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }
}
