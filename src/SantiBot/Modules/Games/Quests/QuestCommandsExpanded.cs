#nullable disable
using SantiBot.Db.Models;
using SantiBot.Modules.Games.Quests;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Quests")]
    [Group("quest")]
    public partial class ExpandedQuestCommands : SantiModule<ExpandedQuestService>
    {
        // ═══════════════════════════════════════════════════════════
        //  .quest quests — Show all active quests
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Quests()
        {
            var userId = ctx.User.Id;
            var guildId = ctx.Guild.Id;

            var dailies = await _service.GetActiveQuestsAsync(userId, guildId, "Daily");
            var weeklies = await _service.GetActiveQuestsAsync(userId, guildId, "Weekly");
            var story = await _service.GetActiveQuestsAsync(userId, guildId, "Story");

            if (dailies.Count == 0 && weeklies.Count == 0 && story.Count == 0)
            {
                await Response().Confirm(
                    "You have no active quests! Use `.quest daily` to get daily quests, " +
                    "`.quest weekly` for a weekly quest, or `.quest story` to start the story chain.")
                    .SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\uD83D\uDCDC Active Quests")
                .WithOkColor();

            if (dailies.Count > 0)
            {
                var dailyText = string.Join("\n", dailies.Select(q =>
                    $"{StatusEmoji(q)} **{q.QuestName}**\n" +
                    $"  {q.Description}\n" +
                    $"  {ExpandedQuestService.ProgressBar(q.CurrentProgress, q.RequiredProgress)}\n" +
                    $"  Rewards: {q.XpReward} XP | {q.CurrencyReward} \uD83C\uDF3B"));
                eb.AddField("\u2600\uFE0F Daily Quests", dailyText, false);
            }

            if (weeklies.Count > 0)
            {
                var weeklyText = string.Join("\n", weeklies.Select(q =>
                    $"{StatusEmoji(q)} **{q.QuestName}**\n" +
                    $"  {q.Description}\n" +
                    $"  {ExpandedQuestService.ProgressBar(q.CurrentProgress, q.RequiredProgress)}\n" +
                    $"  Rewards: {q.XpReward} XP | {q.CurrencyReward} \uD83C\uDF3B" +
                    (q.ExpiresAt.HasValue ? $"\n  Expires: {TimestampTag.FromDateTime(q.ExpiresAt.Value, TimestampTagStyles.Relative)}" : "")));
                eb.AddField("\uD83D\uDCC5 Weekly Quests", weeklyText, false);
            }

            if (story.Count > 0)
            {
                var storyText = string.Join("\n", story.Select(q =>
                    $"{StatusEmoji(q)} **{q.QuestName}**\n" +
                    $"  _{q.Description}_\n" +
                    $"  {ExpandedQuestService.ProgressBar(q.CurrentProgress, q.RequiredProgress)}\n" +
                    $"  Rewards: {q.XpReward} XP | {q.CurrencyReward} \uD83C\uDF3B"));
                eb.AddField("\uD83D\uDCD6 Story Quest", storyText, false);
            }

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  .quest log — Show lifetime quest stats
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuestStats()
        {
            var userId = ctx.User.Id;
            var guildId = ctx.Guild.Id;
            var log = await _service.GetOrCreateQuestLogAsync(userId, guildId);

            var completedStory = await _service.GetCompletedQuestsAsync(userId, guildId, "Story");
            var storyProgress = _service.GetStoryProgress(completedStory);

            var eb = CreateEmbed()
                .WithTitle($"\uD83D\uDCCA Quest Log — {ctx.User.Username}")
                .WithOkColor()
                .AddField("Total Completed", log.TotalQuestsCompleted.ToString(), true)
                .AddField("Daily Completed", log.DailyQuestsCompleted.ToString(), true)
                .AddField("Weekly Completed", log.WeeklyQuestsCompleted.ToString(), true)
                .AddField("Story Progress", $"{storyProgress}/{_service.TotalStoryQuests}", true)
                .AddField("Current Streak", $"\uD83D\uDD25 {log.CurrentStreak} days", true)
                .AddField("Best Streak", $"\uD83C\uDFC6 {log.BestStreak} days", true);

            if (log.LastDailyRefresh > DateTime.MinValue)
                eb.AddField("Last Daily Refresh", TimestampTag.FromDateTime(log.LastDailyRefresh, TimestampTagStyles.Relative).ToString(), true);

            if (log.LastWeeklyRefresh > DateTime.MinValue)
                eb.AddField("Last Weekly Refresh", TimestampTag.FromDateTime(log.LastWeeklyRefresh, TimestampTagStyles.Relative).ToString(), true);

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  .quest daily — Get/view daily quests
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task DailyQuests()
        {
            var userId = ctx.User.Id;
            var guildId = ctx.Guild.Id;

            var dailies = await _service.GenerateDailyQuestsAsync(userId, guildId);

            if (dailies.Count == 0)
            {
                await Response().Error("Failed to generate daily quests. Try again later.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\u2600\uFE0F Daily Quests")
                .WithDescription("Complete all 3 for bonus streak points!\nUse `.quest complete <quest_id>` to claim rewards.")
                .WithOkColor();

            var i = 1;
            foreach (var q in dailies)
            {
                eb.AddField(
                    $"{StatusEmoji(q)} #{i} — {q.QuestName}",
                    $"{q.Description}\n" +
                    $"{ExpandedQuestService.ProgressBar(q.CurrentProgress, q.RequiredProgress)}\n" +
                    $"Rewards: **{q.XpReward}** XP | **{q.CurrencyReward}** \uD83C\uDF3B\n" +
                    $"Expires: {TimestampTag.FromDateTime(q.ExpiresAt.Value, TimestampTagStyles.Relative)}\n" +
                    $"ID: `{q.QuestId}`",
                    false);
                i++;
            }

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  .quest weekly — Get/view weekly quest
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WeeklyQuest()
        {
            var userId = ctx.User.Id;
            var guildId = ctx.Guild.Id;

            var weekly = await _service.GenerateWeeklyQuestAsync(userId, guildId);

            if (weekly is null)
            {
                await Response().Error("Failed to generate weekly quest. Try again later.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\uD83D\uDCC5 Weekly Quest")
                .WithDescription($"{StatusEmoji(weekly)} **{weekly.QuestName}**")
                .WithOkColor()
                .AddField("Objective", weekly.Description, false)
                .AddField("Progress", ExpandedQuestService.ProgressBar(weekly.CurrentProgress, weekly.RequiredProgress), false)
                .AddField("Rewards", $"**{weekly.XpReward}** XP | **{weekly.CurrencyReward}** \uD83C\uDF3B", true);

            if (weekly.ExpiresAt.HasValue)
                eb.AddField("Expires", TimestampTag.FromDateTime(weekly.ExpiresAt.Value, TimestampTagStyles.Relative).ToString(), true);

            eb.AddField("Quest ID", $"`{weekly.QuestId}`", true);

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  .quest story — View/start story quest chain
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task StoryQuest()
        {
            var userId = ctx.User.Id;
            var guildId = ctx.Guild.Id;

            var story = await _service.GetOrStartStoryQuestAsync(userId, guildId);

            if (story is null)
            {
                var eb = CreateEmbed()
                    .WithTitle("\uD83D\uDCD6 Story Complete!")
                    .WithDescription(
                        "You have completed all story quests! You are a true legend.\n\n" +
                        "\u2B50 **The Legend Continues** \u2B50\n" +
                        "Your name will be remembered through the ages.")
                    .WithOkColor();
                await Response().Embed(eb).SendAsync();
                return;
            }

            var completedStory = await _service.GetCompletedQuestsAsync(userId, guildId, "Story");
            var progress = _service.GetStoryProgress(completedStory);

            var embed = CreateEmbed()
                .WithTitle($"\uD83D\uDCD6 Story Quest — Chapter {progress + 1}/{_service.TotalStoryQuests}")
                .WithDescription(
                    $"**{story.QuestName}**\n\n" +
                    $"_{story.Description}_")
                .WithOkColor()
                .AddField("Progress", ExpandedQuestService.ProgressBar(story.CurrentProgress, story.RequiredProgress), false)
                .AddField("Rewards", $"**{story.XpReward}** XP | **{story.CurrencyReward}** \uD83C\uDF3B", true)
                .AddField("Quest ID", $"`{story.QuestId}`", true);

            // Show completed chapters
            if (completedStory.Count > 0)
            {
                var completed = string.Join("\n", completedStory
                    .OrderBy(x => x.QuestId)
                    .Select(q => $"\u2705 ~~{q.QuestName}~~"));
                embed.AddField("Completed Chapters", completed, false);
            }

            await Response().Embed(embed).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  .quest factions — Show all faction standings
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Factions()
        {
            var userId = ctx.User.Id;
            var guildId = ctx.Guild.Id;

            var standings = await _service.GetAllFactionStandingsAsync(userId, guildId);

            var eb = CreateEmbed()
                .WithTitle("\u2694\uFE0F Faction Standings")
                .WithDescription("Complete quests aligned with a faction to gain reputation!")
                .WithOkColor();

            foreach (var s in standings)
            {
                var emoji = ExpandedQuestService.GetFactionEmoji(s.FactionName);
                var nextRank = ExpandedQuestService.GetNextRank(s.Rank);
                var nextThreshold = ExpandedQuestService.GetNextRankThreshold(s.Rank);

                var progressText = nextRank is not null
                    ? $"{ExpandedQuestService.ProgressBar(s.Reputation, nextThreshold)}\nNext rank: **{nextRank}** at {nextThreshold} rep"
                    : $"Rep: **{s.Reputation}** — MAX RANK!";

                eb.AddField(
                    $"{emoji} {s.FactionName} — {s.Rank}",
                    progressText,
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  .quest factioninfo <faction> — Detailed faction info
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FactionInfo([Leftover] string factionName)
        {
            if (string.IsNullOrWhiteSpace(factionName))
            {
                await Response().Error("Please specify a faction name: `Warriors Guild`, `Merchants Guild`, `Scholars Circle`, or `Shadow Brotherhood`").SendAsync();
                return;
            }

            // Fuzzy match faction name
            var matched = ExpandedQuestService.FactionNames
                .FirstOrDefault(f => f.Contains(factionName, StringComparison.OrdinalIgnoreCase));

            if (matched is null)
            {
                await Response().Error(
                    $"Unknown faction: **{factionName}**\n" +
                    "Available factions: `Warriors Guild`, `Merchants Guild`, `Scholars Circle`, `Shadow Brotherhood`")
                    .SendAsync();
                return;
            }

            var userId = ctx.User.Id;
            var guildId = ctx.Guild.Id;
            var standing = await _service.GetFactionStandingAsync(userId, guildId, matched);
            var desc = ExpandedQuestService.GetFactionDescription(matched);
            var emoji = ExpandedQuestService.GetFactionEmoji(matched);
            var nextRank = ExpandedQuestService.GetNextRank(standing.Rank);
            var nextThreshold = ExpandedQuestService.GetNextRankThreshold(standing.Rank);

            var eb = CreateEmbed()
                .WithTitle($"{emoji} {matched}")
                .WithDescription(desc)
                .WithOkColor()
                .AddField("Your Rank", standing.Rank, true)
                .AddField("Reputation", standing.Reputation.ToString(), true);

            if (nextRank is not null)
            {
                eb.AddField("Next Rank", $"**{nextRank}** at {nextThreshold} rep", true);
                eb.AddField("Progress", ExpandedQuestService.ProgressBar(standing.Reputation, nextThreshold), false);
            }
            else
            {
                eb.AddField("Status", "\u2B50 MAX RANK — You are a Legend!", true);
            }

            eb.AddField("Ranks",
                "Outsider (0) \u2192 Initiate (100) \u2192 Member (500) \u2192 Veteran (1500) \u2192 Champion (4000) \u2192 Legend (10000)",
                false);

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  .quest complete <quest_id> — Claim a completed quest
        // ═══════════════════════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuestComplete([Leftover] string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                await Response().Error("Please specify a quest ID. Use `.quest quests` to see your active quests and their IDs.").SendAsync();
                return;
            }

            questId = questId.Trim();
            var userId = ctx.User.Id;
            var guildId = ctx.Guild.Id;

            var (success, quest, currencyAwarded) = await _service.CompleteQuestAsync(userId, guildId, questId);

            if (quest is null)
            {
                await Response().Error($"No active quest found with ID `{questId}`. Use `.quest quests` to see your quests.").SendAsync();
                return;
            }

            if (!success)
            {
                await Response().Error(
                    $"**{quest.QuestName}** is not complete yet!\n" +
                    $"{ExpandedQuestService.ProgressBar(quest.CurrentProgress, quest.RequiredProgress)}")
                    .SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\uD83C\uDF89 Quest Complete!")
                .WithDescription(
                    $"**{quest.QuestName}** has been completed!\n\n" +
                    $"_{quest.Description}_")
                .WithOkColor()
                .AddField("Rewards Claimed",
                    $"**{quest.XpReward}** XP\n" +
                    $"**{currencyAwarded}** \uD83C\uDF3B",
                    false);

            if (quest.Type == "Story")
            {
                var nextStory = await _service.GetOrStartStoryQuestAsync(userId, guildId);
                if (nextStory is not null)
                    eb.AddField("Next Chapter", $"**{nextStory.QuestName}** — _{nextStory.Description}_", false);
                else
                    eb.AddField("Story Complete!", "\u2B50 You have completed the entire story quest chain!", false);
            }

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        private static string StatusEmoji(QuestProgress q)
            => q.Status switch
            {
                "Completed" => "\u2705",
                "Failed" => "\u274C",
                _ => q.CurrentProgress >= q.RequiredProgress ? "\uD83C\uDF1F" : "\uD83D\uDD38"
            };
    }
}
