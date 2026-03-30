#nullable disable
using SantiBot.Modules.Games.Seasonal;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Seasonal")]
    [Group("season")]
    public partial class SeasonalCommands : SantiModule<SeasonalService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Current()
        {
            var ev = _service.GetActiveEvent();
            if (ev is null)
            {
                await Response()
                    .Confirm("No seasonal event is active right now. Check `.season calendar` to see upcoming events!")
                    .SendAsync();
                return;
            }

            var (xp, loot) = _service.GetSeasonalMultiplier();
            var boss = ev.Boss;

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{ev.Emoji} {ev.Name} {ev.Emoji}")
                .WithDescription(ev.Greeting)
                .AddField("Seasonal Currency",
                    $"{ev.CurrencyEmoji} {ev.SeasonalCurrency}", true)
                .AddField("XP Multiplier",
                    $"**{xp:0.0}x**", true)
                .AddField("Loot Multiplier",
                    $"**{loot:0.0}x**", true)
                .AddField("Raid Boss",
                    $"{boss.Emoji} **{boss.Name}**\n"
                    + $"HP: {boss.Hp:N0} | ATK: {boss.Atk} | DEF: {boss.Def}", false)
                .AddField("Achievement",
                    $"\U0001f3c6 {ev.Achievement}", true)
                .AddField("Dates",
                    $"{ev.StartMonth}/{ev.StartDay} - {ev.EndMonth}/{ev.EndDay}", true)
                .WithFooter("Use .season boss to view boss details | .season calendar for all events");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Calendar()
        {
            var events = _service.GetEventCalendar();

            var lines = new List<string>();
            foreach (var ev in events)
            {
                var active = _service.IsEventActive(ev.Name) ? " **[ACTIVE]**" : "";
                lines.Add(
                    $"{ev.Emoji} **{ev.Name}** -- "
                    + $"{ev.StartMonth}/{ev.StartDay} to {ev.EndMonth}/{ev.EndDay}"
                    + $" | {ev.CurrencyEmoji} {ev.SeasonalCurrency}"
                    + active);
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\U0001f4c5 Seasonal Event Calendar")
                .WithDescription(string.Join("\n", lines))
                .WithFooter("Use .season current during an active event for full details");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Boss([Leftover] string eventName = null)
        {
            SeasonalService.SeasonalBoss boss;
            string title;

            if (string.IsNullOrWhiteSpace(eventName))
            {
                var ev = _service.GetActiveEvent();
                if (ev is null)
                {
                    await Response()
                        .Error("No seasonal event is active. Specify an event name: `.season boss Christmas`")
                        .SendAsync();
                    return;
                }
                boss = ev.Boss;
                title = $"{ev.Emoji} {ev.Name} Raid Boss";
            }
            else
            {
                boss = _service.GetSeasonalBoss(eventName);
                if (boss is null)
                {
                    await Response()
                        .Error($"No event found with name \"{eventName}\". Use `.season calendar` to see all events.")
                        .SendAsync();
                    return;
                }
                title = $"Raid Boss: {boss.Name}";
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{boss.Emoji} {title}")
                .AddField("Name", boss.Name, true)
                .AddField("HP", $"{boss.Hp:N0}", true)
                .AddField("Attack", $"{boss.Atk}", true)
                .AddField("Defense", $"{boss.Def}", true)
                .WithFooter("Gather your guild and take down the seasonal boss!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Advent(int day = 0)
        {
            if (day == 0)
                day = DateTime.UtcNow.Day;

            var (success, message) = _service.GetAdventCalendarReward(day);
            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\U0001f384 Advent Calendar")
                .WithDescription(message)
                .WithFooter("Come back every day in December for a new reward!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EggHunt()
        {
            var (success, message) = await _service.EggHunt(ctx.User.Id, ctx.Guild.Id);
            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\U0001f430 Easter Egg Hunt!")
                .WithDescription(message)
                .WithFooter("Hunt again to find rarer eggs!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SeasonalShop()
        {
            var ev = _service.GetActiveEvent();
            if (ev is null)
            {
                await Response()
                    .Error("No seasonal event is active. The Seasonal Shop only opens during events!")
                    .SendAsync();
                return;
            }

            var shopItems = ev.Name switch
            {
                "Valentine's Day" =>
                    $"{ev.CurrencyEmoji} **Cupid's Bow** -- 200 Hearts\n"
                    + $"{ev.CurrencyEmoji} **Love Potion** -- 100 Hearts\n"
                    + $"{ev.CurrencyEmoji} **Rose Bouquet Skin** -- 500 Hearts\n"
                    + $"{ev.CurrencyEmoji} **Couple's Ring** -- 750 Hearts",

                "St. Patrick's Day" =>
                    $"{ev.CurrencyEmoji} **Lucky Clover Charm** -- 150 Gold Coins\n"
                    + $"{ev.CurrencyEmoji} **Leprechaun Hat** -- 300 Gold Coins\n"
                    + $"{ev.CurrencyEmoji} **Rainbow Trail** -- 500 Gold Coins\n"
                    + $"{ev.CurrencyEmoji} **Pot of Gold Pet** -- 800 Gold Coins",

                "Easter" =>
                    $"{ev.CurrencyEmoji} **Bunny Ears** -- 100 Chocolate\n"
                    + $"{ev.CurrencyEmoji} **Easter Basket** -- 200 Chocolate\n"
                    + $"{ev.CurrencyEmoji} **Bunny Pet** -- 400 Chocolate\n"
                    + $"{ev.CurrencyEmoji} **Golden Carrot Sword** -- 600 Chocolate",

                "Summer Festival" =>
                    $"{ev.CurrencyEmoji} **Surfboard Mount** -- 300 Seashells\n"
                    + $"{ev.CurrencyEmoji} **Beach Umbrella Shield** -- 200 Seashells\n"
                    + $"{ev.CurrencyEmoji} **Tropical Fish Pet** -- 400 Seashells\n"
                    + $"{ev.CurrencyEmoji} **Tidal Wave Spell** -- 700 Seashells",

                "Halloween" =>
                    $"{ev.CurrencyEmoji} **Ghost Skin** -- 200 Candy\n"
                    + $"{ev.CurrencyEmoji} **Vampire Skin** -- 300 Candy\n"
                    + $"{ev.CurrencyEmoji} **Werewolf Skin** -- 300 Candy\n"
                    + $"{ev.CurrencyEmoji} **Pumpkin Head** -- 500 Candy\n"
                    + $"{ev.CurrencyEmoji} **Haunted Scythe** -- 800 Candy",

                "Thanksgiving" =>
                    $"{ev.CurrencyEmoji} **Harvest Crown** -- 150 Feast Tokens\n"
                    + $"{ev.CurrencyEmoji} **Cornucopia Shield** -- 300 Feast Tokens\n"
                    + $"{ev.CurrencyEmoji} **Feast Buff (2hr)** -- 200 Feast Tokens\n"
                    + $"{ev.CurrencyEmoji} **Pilgrim Outfit** -- 500 Feast Tokens",

                "Christmas" =>
                    $"{ev.CurrencyEmoji} **Santa Hat** -- 100 Snowflakes\n"
                    + $"{ev.CurrencyEmoji} **Candy Cane Sword** -- 250 Snowflakes\n"
                    + $"{ev.CurrencyEmoji} **Reindeer Pet** -- 400 Snowflakes\n"
                    + $"{ev.CurrencyEmoji} **Santa's Sleigh Mount** -- 800 Snowflakes\n"
                    + $"{ev.CurrencyEmoji} **Gift Box (random legendary)** -- 1500 Snowflakes",

                "New Year" =>
                    $"{ev.CurrencyEmoji} **Party Hat** -- 100 Fireworks\n"
                    + $"{ev.CurrencyEmoji} **Confetti Cannon** -- 200 Fireworks\n"
                    + $"{ev.CurrencyEmoji} **Midnight Cloak** -- 500 Fireworks\n"
                    + $"{ev.CurrencyEmoji} **Father Time's Hourglass** -- 1000 Fireworks",

                _ => "No shop items available for this event."
            };

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{ev.Emoji} {ev.Name} Seasonal Shop")
                .WithDescription(shopItems)
                .WithFooter($"Earn {ev.SeasonalCurrency} by participating in seasonal activities!");

            await Response().Embed(eb).SendAsync();
        }
    }
}
