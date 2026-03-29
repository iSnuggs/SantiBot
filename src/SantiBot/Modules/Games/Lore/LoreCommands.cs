#nullable disable
using SantiBot.Modules.Games.Lore;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Lore")]
    [Group("lore")]
    public partial class LoreCommands(LoreService _ls, ICurrencyProvider _cp) : SantiModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bestiary()
        {
            var monsters = await _ls.GetBestiaryAsync(ctx.Guild.Id, ctx.User.Id);
            var allMonsters = LoreService.DefaultLore.Count(x => x.Category == "Monster");

            if (monsters.Count == 0)
            {
                await Response().Confirm(
                    $"\ud83d\udcd6 **{ctx.User.Username}'s Bestiary** (0/{allMonsters})\n\n"
                    + "Your bestiary is empty! Defeat monsters in dungeons to discover their lore entries."
                ).SendAsync();
                return;
            }

            var lines = monsters
                .Select(m => $"{m.Emoji} **{m.EntryName}** - {Truncate(m.Description, 80)}")
                .ToList();

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\ud83d\udcd6 {ctx.User.Username}'s Bestiary ({monsters.Count}/{allMonsters})")
                .WithDescription(string.Join("\n", lines))
                .WithFooter("Use .lore read <name> for full descriptions");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task LoreBook([Leftover] string category = null)
        {
            var validCategories = new[] { "Monster", "Item", "Location", "Boss", "NPC", "History" };

            if (!string.IsNullOrEmpty(category))
            {
                category = validCategories.FirstOrDefault(c =>
                    c.Equals(category, StringComparison.OrdinalIgnoreCase));

                if (category is null)
                {
                    await Response().Confirm(
                        $"Invalid category. Valid categories: {string.Join(", ", validCategories)}"
                    ).SendAsync();
                    return;
                }
            }

            var discovered = await _ls.GetDiscoveredLoreAsync(ctx.Guild.Id, ctx.User.Id, category);
            var (totalDiscovered, totalEntries, _) = await _ls.GetLoreStatsAsync(ctx.Guild.Id, ctx.User.Id);

            if (discovered.Count == 0)
            {
                var filterMsg = string.IsNullOrEmpty(category) ? "" : $" in **{category}**";
                await Response().Confirm(
                    $"\ud83d\udcda **{ctx.User.Username}'s Lore Book**\n\n"
                    + $"No entries discovered{filterMsg}! Explore dungeons, defeat bosses, and complete quests to fill your lore book."
                ).SendAsync();
                return;
            }

            var grouped = discovered.GroupBy(e => e.Category).OrderBy(g => g.Key);
            var description = "";

            foreach (var group in grouped)
            {
                var categoryEmoji = group.Key switch
                {
                    "Monster" => "\ud83d\udc7e",
                    "Item" => "\ud83d\udce6",
                    "Location" => "\ud83d\uddfa\ufe0f",
                    "Boss" => "\ud83d\udc79",
                    "NPC" => "\ud83e\uddd1\u200d\ud83e\udd1d\u200d\ud83e\uddd1",
                    "History" => "\ud83d\udcdc",
                    _ => "\ud83d\udcd6"
                };
                description += $"\n**{categoryEmoji} {group.Key}**\n";
                foreach (var entry in group)
                {
                    description += $"  {entry.Emoji} {entry.EntryName}\n";
                }
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\ud83d\udcda {ctx.User.Username}'s Lore Book ({totalDiscovered}/{totalEntries})")
                .WithDescription(description.Trim())
                .WithFooter("Use .lore read <name> for full descriptions");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task LoreRead([Leftover] string entryName)
        {
            if (string.IsNullOrWhiteSpace(entryName))
            {
                await Response().Confirm("Please specify a lore entry name. Example: `.lore read Shadow Rat`").SendAsync();
                return;
            }

            var entry = await _ls.GetLoreEntryAsync(ctx.Guild.Id, entryName);
            if (entry is null)
            {
                await Response().Confirm($"No lore entry found matching **{entryName}**. Check your spelling or use `.lore book` to see your discovered entries.").SendAsync();
                return;
            }

            var hasDiscovered = await _ls.HasDiscoveredAsync(ctx.Guild.Id, ctx.User.Id, entryName);
            if (!hasDiscovered)
            {
                await Response().Confirm(
                    $"{entry.Emoji} **{entry.EntryName}** [{entry.Category}]\n\n"
                    + "??? This entry has not been discovered yet. Explore the dungeon to unlock its secrets!"
                ).SendAsync();
                return;
            }

            var discoveredBy = entry.IsDiscovered && entry.DiscoveredBy != 0
                ? $"\nFirst discovered by: <@{entry.DiscoveredBy}>"
                : "";

            var discoveredAt = entry.DiscoveredAt.HasValue
                ? $"\nFirst discovery: {entry.DiscoveredAt.Value:yyyy-MM-dd HH:mm} UTC"
                : "";

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{entry.Emoji} {entry.EntryName}")
                .WithDescription(entry.Description)
                .AddField("Category", entry.Category, true)
                .WithFooter($"{discoveredBy}{discoveredAt}");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TreasureMap()
        {
            // Check for existing map first
            var existing = await _ls.GetUserMapAsync(ctx.Guild.Id, ctx.User.Id);
            if (existing is not null)
            {
                var remaining = existing.ExpiresAt - DateTime.UtcNow;
                var sign = _cp.GetCurrencySign();

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle($"\ud83d\uddfa\ufe0f {existing.MapName}")
                    .AddField("Clue 1", existing.Clue1)
                    .AddField("Clue 2", existing.Clue2)
                    .AddField("Clue 3", existing.Clue3)
                    .AddField("Rewards",
                        $"**{existing.RewardCurrency}**{sign} + **{existing.RewardXp}** XP + **{existing.RewardItemName}**")
                    .WithFooter($"Expires in {remaining.Hours}h {remaining.Minutes}m | Solve with .lore treasuresolve <answer>");

                await Response().Embed(eb).SendAsync();
                return;
            }

            // Generate new map
            var map = await _ls.GenerateTreasureMapAsync(ctx.Guild.Id, ctx.User.Id);
            if (map is null)
            {
                await Response().Confirm("You already have an active treasure map! Use `.lore treasuremap` to view it.").SendAsync();
                return;
            }

            var csign = _cp.GetCurrencySign();
            var mapEmbed = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\ud83d\uddfa\ufe0f New Treasure Map: {map.MapName}")
                .WithDescription("You found a mysterious treasure map! Solve the riddle to claim the reward.")
                .AddField("Clue 1", map.Clue1)
                .AddField("Clue 2", map.Clue2)
                .AddField("Clue 3", map.Clue3)
                .AddField("Rewards",
                    $"**{map.RewardCurrency}**{csign} + **{map.RewardXp}** XP + **{map.RewardItemName}**")
                .WithFooter("Expires in 24 hours | Solve with .lore treasuresolve <answer>");

            await Response().Embed(mapEmbed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TreasureSolve([Leftover] string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                await Response().Confirm("Please provide an answer! Example: `.lore treasuresolve thornwall`").SendAsync();
                return;
            }

            var (success, map, error) = await _ls.SolveTreasureMapAsync(ctx.Guild.Id, ctx.User.Id, answer);

            if (error == "no_map")
            {
                await Response().Confirm("You don't have an active treasure map! Use `.lore treasuremap` to get one.").SendAsync();
                return;
            }

            if (error == "invalid_map")
            {
                await Response().Confirm("Something went wrong with your map. Try getting a new one.").SendAsync();
                return;
            }

            if (error == "wrong_answer")
            {
                await Response().Confirm(
                    $"\u274c Wrong answer! The treasure remains hidden.\n"
                    + $"Re-read your clues with `.lore treasuremap` and try again."
                ).SendAsync();
                return;
            }

            var sign = _cp.GetCurrencySign();
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\ud83c\udf89 Treasure Found: {map.MapName}")
                .WithDescription(
                    $"{ctx.User.Mention} solved the treasure map!\n\n"
                    + $"\ud83d\udcb0 **{map.RewardCurrency}**{sign}\n"
                    + $"\u2b50 **{map.RewardXp}** XP\n"
                    + $"\ud83c\udf81 **{map.RewardItemName}**");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WorldEvents()
        {
            var events = await _ls.GetActiveEventsAsync(ctx.Guild.Id);

            if (events.Count == 0)
            {
                await Response().Confirm(
                    "\ud83c\udf0d **World Events**\n\n"
                    + "No active world events right now. The realm is peaceful... for now."
                ).SendAsync();
                return;
            }

            var lines = events.Select(e =>
            {
                var remaining = e.EndsAt - DateTime.UtcNow;
                var typeEmoji = e.EventType switch
                {
                    "Invasion" => "\u2694\ufe0f",
                    "Festival" => "\ud83c\udf89",
                    "Eclipse" => "\ud83c\udf11",
                    "Storm" => "\u26c8\ufe0f",
                    "Plague" => "\u2620\ufe0f",
                    "Blessing" => "\u2728",
                    _ => "\ud83c\udf0d"
                };
                return $"{typeEmoji} **{e.EventName}** ({e.EventType})\n"
                    + $"  {e.Description}\n"
                    + $"  Bonus: *{e.BonusEffect}*\n"
                    + $"  Ends in: {remaining.Hours}h {remaining.Minutes}m";
            });

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\ud83c\udf0d Active World Events")
                .WithDescription(string.Join("\n\n", lines));

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WorldHistory()
        {
            var history = await _ls.GetDiscoveredLoreAsync(ctx.Guild.Id, ctx.User.Id, "History");
            var allHistory = LoreService.DefaultLore.Count(x => x.Category == "History");

            if (history.Count == 0)
            {
                await Response().Confirm(
                    "\ud83d\udcdc **World History** (0/{allHistory})\n\n"
                    + "You haven't discovered any history entries yet. Explore ancient locations and speak to NPCs to learn the world's story."
                ).SendAsync();
                return;
            }

            var lines = history
                .Select(h => $"{h.Emoji} **{h.EntryName}**\n{Truncate(h.Description, 150)}")
                .ToList();

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\ud83d\udcdc World History Timeline ({history.Count}/{allHistory})")
                .WithDescription(string.Join("\n\n", lines))
                .WithFooter("Use .lore read <name> for the full account");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task LoreStats()
        {
            var (discovered, total, byCategory) = await _ls.GetLoreStatsAsync(ctx.Guild.Id, ctx.User.Id);
            var percentage = total > 0 ? (discovered * 100 / total) : 0;

            // Build progress bar
            var filled = percentage / 5;
            var progressBar = new string('\u2588', filled) + new string('\u2591', 20 - filled);

            var categoryLines = byCategory
                .OrderBy(kvp => kvp.Key)
                .Select(kvp =>
                {
                    var catEmoji = kvp.Key switch
                    {
                        "Boss" => "\ud83d\udc79",
                        "History" => "\ud83d\udcdc",
                        "Item" => "\ud83d\udce6",
                        "Location" => "\ud83d\uddfa\ufe0f",
                        "Monster" => "\ud83d\udc7e",
                        "NPC" => "\ud83e\uddd1\u200d\ud83e\udd1d\u200d\ud83e\uddd1",
                        _ => "\ud83d\udcd6"
                    };
                    var catPct = kvp.Value.max > 0 ? (kvp.Value.found * 100 / kvp.Value.max) : 0;
                    return $"{catEmoji} **{kvp.Key}**: {kvp.Value.found}/{kvp.Value.max} ({catPct}%)";
                });

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\ud83d\udcca {ctx.User.Username}'s Lore Discovery Progress")
                .WithDescription(
                    $"**Overall Progress: {discovered}/{total} ({percentage}%)**\n"
                    + $"`[{progressBar}]`\n\n"
                    + string.Join("\n", categoryLines))
                .WithFooter("Discover lore by exploring dungeons, defeating monsters, and completing quests!");

            await Response().Embed(eb).SendAsync();
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text[..(maxLength - 3)] + "...";
        }
    }
}
