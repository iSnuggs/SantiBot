#nullable disable
using SantiBot.Modules.Games.Dungeon;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Dungeon")]
    [Group("dungeon")]
    public partial class DungeonCommands : SantiModule<DungeonService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Enter(int difficulty = 1)
        {
            var (success, message) = await _service.EnterDungeonAsync(
                ctx.Channel.Id, ctx.Guild.Id, ctx.User.Id, ctx.User.Username, difficulty);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Invite(IUser user)
        {
            var (success, message) = await _service.InviteToPartyAsync(
                ctx.Channel.Id, ctx.Guild.Id, user.Id, user.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Explore()
        {
            var (success, message) = await _service.ExploreAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Attack()
        {
            var (success, message) = await _service.AttackAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Flee()
        {
            var (success, message) = _service.Flee(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ChooseClass([Leftover] string className = null)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                var eb = CreateEmbed()
                    .WithTitle("⚔️ Choose Your Class (11 available)")
                    .WithOkColor();

                foreach (var (name, (hpMult, atkMult, defMult, desc, emoji)) in DungeonService.Classes)
                    eb.AddField($"{emoji} {name}", $"{desc}\nHP: {hpMult}% | ATK: {atkMult}% | DEF: {defMult}%", false);

                eb.WithFooter("Use .dungeon chooseclass <name> to pick!");
                await Response().Embed(eb).SendAsync();
                return;
            }

            var (success, message) = await _service.ChooseClassAsync(ctx.User.Id, ctx.Guild.Id, className);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ChooseRace([Leftover] string raceName = null)
        {
            if (string.IsNullOrWhiteSpace(raceName))
            {
                var eb = CreateEmbed()
                    .WithTitle("🧬 Choose Your Race (8 available)")
                    .WithOkColor();

                foreach (var (name, (hpMod, atkMod, defMod, desc, emoji, ability)) in DungeonService.Races)
                    eb.AddField($"{emoji} {name}", $"{desc}\nRacial: **{ability}**\nHP: {hpMod}% | ATK: {atkMod}% | DEF: {defMod}%", false);

                eb.WithFooter("Use .dungeon chooserace <name> to pick!");
                await Response().Embed(eb).SendAsync();
                return;
            }

            var (success, message) = await _service.ChooseRaceAsync(ctx.User.Id, ctx.Guild.Id, raceName);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Profile(IUser target = null)
        {
            var user = target ?? ctx.User;
            var player = await _service.GetOrCreatePlayerAsync(user.Id, ctx.Guild.Id);
            var equipped = await _service.GetEquippedItemsAsync(user.Id, ctx.Guild.Id);
            var (maxHp, atk, def) = DungeonService.GetEffectiveStats(player, equipped);

            var classEmoji = DungeonService.Classes.TryGetValue(player.Class, out var cls) ? cls.Emoji : "⚔️";
            var raceEmoji = DungeonService.Races.TryGetValue(player.Race, out var race) ? race.Emoji : "🧑";
            var xpNeeded = DungeonService.XpForLevel(player.Level + 1);
            var xpProgress = player.Xp;
            for (var i = 1; i < player.Level; i++)
                xpProgress -= DungeonService.XpForLevel(i);

            var barLength = 10;
            var filled = xpNeeded > 0 ? (int)(xpProgress * barLength / xpNeeded) : barLength;
            filled = Math.Clamp(filled, 0, barLength);
            var xpBar = new string('█', filled) + new string('░', barLength - filled);

            var eb = CreateEmbed()
                .WithTitle($"{raceEmoji}{classEmoji} {user.Username} — Dungeon Profile")
                .AddField("Race", $"{raceEmoji} {player.Race}", true)
                .AddField("Class", $"{classEmoji} {player.Class}", true)
                .AddField("Level", $"**{player.Level}**", true)
                .AddField("XP", $"[{xpBar}] {xpProgress}/{xpNeeded}", false)
                .AddField("Stats", $"❤️ HP: **{maxHp}** | ⚔️ ATK: **{atk}** | 🛡️ DEF: **{def}**", false)
                .AddField("Lifetime",
                    $"🏰 Dungeons: **{player.DungeonsCleared}** | 💀 Kills: **{player.MonstersKilled}**\n" +
                    $"🥠 Loot: **{player.TotalLoot}** | ☠️ Deaths: **{player.DeathCount}**\n" +
                    $"🏆 Highest Difficulty: **{player.HighestDifficulty}**", false)
                .WithOkColor();

            // Racial ability
            if (DungeonService.Races.TryGetValue(player.Race, out var raceInfo))
                eb.AddField("Racial Ability", $"**{raceInfo.Ability}** — {raceInfo.Desc}", false);

            // Equipment
            var weaponName = equipped.FirstOrDefault(e => e.Slot == "Weapon")?.Name ?? "None";
            var armorName = equipped.FirstOrDefault(e => e.Slot == "Armor")?.Name ?? "None";
            var accName = equipped.FirstOrDefault(e => e.Slot == "Accessory")?.Name ?? "None";
            eb.AddField("Equipment",
                $"🗡️ Weapon: **{weaponName}**\n" +
                $"🛡️ Armor: **{armorName}**\n" +
                $"💎 Accessory: **{accName}**", false);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Inventory()
        {
            var items = await _service.GetInventoryAsync(ctx.User.Id, ctx.Guild.Id);

            if (items.Count == 0)
            {
                await Response().Error("Your inventory is empty! Clear some dungeons to find loot.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"🎒 {ctx.User.Username}'s Inventory ({items.Count} items)")
                .WithOkColor();

            var sb = new System.Text.StringBuilder();
            foreach (var item in items)
            {
                var equippedTag = item.IsEquipped ? " **[EQUIPPED]**" : "";
                sb.AppendLine($"{DungeonService.RarityEmoji(item.Rarity)} `ID:{item.Id}` **{item.Name}** ({item.Slot}){equippedTag}");
                var stats = new List<string>();
                if (item.BonusAttack > 0) stats.Add($"ATK+{item.BonusAttack}");
                if (item.BonusDefense > 0) stats.Add($"DEF+{item.BonusDefense}");
                if (item.BonusHp > 0) stats.Add($"HP+{item.BonusHp}");
                if (item.SpecialEffect is not null) stats.Add($"*{item.SpecialEffect}*");
                if (stats.Count > 0) sb.AppendLine($"  {string.Join(" | ", stats)}");
            }

            eb.WithDescription(sb.ToString());
            eb.WithFooter("Use .dungeon equip <id> to equip an item");
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Equip(int itemId)
        {
            var success = await _service.EquipItemAsync(ctx.User.Id, ctx.Guild.Id, itemId);
            if (success)
                await Response().Confirm($"✅ Item equipped! Use `.dungeon profile` to see your stats.").SendAsync();
            else
                await Response().Error("Item not found or can't be equipped.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Status()
        {
            if (!_service.ActiveDungeons.TryGetValue(ctx.Channel.Id, out var run))
            {
                await Response().Error("No active dungeon run! Start one with `.dungeon enter`").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"🏰 Dungeon — Difficulty {run.Difficulty}")
                .AddField("Progress", $"Room {run.CurrentRoom}/{run.MaxRooms}", true)
                .AddField("Loot", $"{run.TotalLoot} 🥠", true)
                .AddField("XP Pool", $"{run.XpPool} XP", true)
                .WithOkColor();

            foreach (var p in run.Party)
            {
                var classEmoji = DungeonService.Classes.TryGetValue(p.Class, out var c) ? c.Emoji : "⚔️";
                var raceEmoji = DungeonService.Races.TryGetValue(p.Race, out var r) ? r.Emoji : "🧑";
                var hpFill = Math.Max(0, p.Hp * 10 / Math.Max(1, p.MaxHp));
                var hpBar = new string('█', hpFill) + new string('░', 10 - hpFill);
                eb.AddField($"{raceEmoji}{classEmoji} {p.Username} (Lv.{p.Level})",
                    $"[{hpBar}] {p.Hp}/{p.MaxHp} HP\nATK: {p.Attack} | DEF: {p.Defense}", true);
            }

            if (run.CurrentMonster is not null)
                eb.AddField("Monster", $"{run.CurrentMonster}\nHP: {run.MonsterHp} | ATK: {run.MonsterAtk} | DEF: {run.MonsterDef}", false);

            if (run.SkeletonHp > 0)
                eb.AddField("Raised Skeleton", $"HP: {run.SkeletonHp} | ATK: {run.SkeletonAtk}", true);

            if (run.LootDrops.Count > 0)
                eb.WithFooter($"📦 {run.LootDrops.Count} item(s) found this run");

            await Response().Embed(eb).SendAsync();
        }
    }
}
