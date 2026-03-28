using SantiBot.Modules.Gambling.Common;
using SantiBot.Modules.Gambling.Fishing;
using SantiBot.Modules.Gambling.Services;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Fishing")]
    [Group("gfish")]
    public partial class FishingCommands : GamblingModule<FishingService>
    {
        private readonly FishingService _fishing;
        private readonly DiscordSocketClient _client;

        public FishingCommands(GamblingConfigService gcs,
            FishingService fishing,
            DiscordSocketClient client) : base(gcs)
        {
            _fishing = fishing;
            _client = client;
        }

        [Cmd]
        public async Task GFish()
        {
            var (fish, cooldown) = await _fishing.CastLineAsync(ctx.User.Id);

            if (cooldown.HasValue)
            {
                await Response().Error(strs.gfish_cooldown(cooldown.Value.Seconds)).SendAsync();
                return;
            }

            if (fish is null)
            {
                await Response().Error(strs.gfish_nothing).SendAsync();
                return;
            }

            var rarityEmoji = fish.Rarity switch
            {
                "Common" => "",
                "Uncommon" => "\u2b50",
                "Rare" => "\u2b50\u2b50",
                "Epic" => "\u2b50\u2b50\u2b50",
                "Legendary" => "\ud83c\udf1f",
                _ => ""
            };

            var weightStr = fish.Weight >= 1000
                ? $"{fish.Weight / 1000.0:0.##}kg"
                : $"{fish.Weight}g";

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.gfish_caught))
                .AddField(GetText(strs.gfish_name), $"{fish.FishName} {rarityEmoji}", true)
                .AddField(GetText(strs.gfish_rarity), fish.Rarity, true)
                .AddField(GetText(strs.gfish_weight), weightStr, true)
                .AddField(GetText(strs.gfish_value), N(fish.SellValue), true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        public async Task FishSell()
        {
            var total = await _fishing.SellAllFishAsync(ctx.User.Id);

            if (total == 0)
            {
                await Response().Error(strs.gfish_bucket_empty).SendAsync();
                return;
            }

            await Response().Confirm(strs.gfish_sold_all(N(total))).SendAsync();
        }

        [Cmd]
        public async Task FishBucket()
        {
            var bucket = await _fishing.GetBucketAsync(ctx.User.Id);

            if (bucket.Count == 0)
            {
                await Response().Error(strs.gfish_bucket_empty).SendAsync();
                return;
            }

            var desc = string.Join('\n', bucket.Select(f =>
            {
                var w = f.Weight >= 1000 ? $"{f.Weight / 1000.0:0.##}kg" : $"{f.Weight}g";
                return $"**{f.FishName}** ({f.Rarity}) - {w} - {N(f.SellValue)}";
            }));

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.gfish_bucket_title))
                .WithDescription(desc);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        public async Task FishRod()
        {
            var (current, next, cost) = await _fishing.GetRodInfoAsync(ctx.User.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.gfish_rod_title))
                .AddField(GetText(strs.gfish_rod_current), current, true);

            if (next is not null)
                eb.AddField(GetText(strs.gfish_rod_next), $"{next} - {N(cost)}", true);
            else
                eb.AddField(GetText(strs.gfish_rod_next), GetText(strs.gfish_rod_max), true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        public async Task FishRodUpgrade()
        {
            var (success, newRod) = await _fishing.UpgradeRodAsync(ctx.User.Id);

            if (!success)
            {
                await Response().Error(strs.gfish_rod_upgrade_fail).SendAsync();
                return;
            }

            await Response().Confirm(strs.gfish_rod_upgraded(newRod!)).SendAsync();
        }

        [Cmd]
        public async Task FishLeaderboard()
        {
            var top = await _fishing.GetLeaderboardAsync();

            if (top.Count == 0)
            {
                await Response().Error(strs.gfish_lb_empty).SendAsync();
                return;
            }

            var desc = string.Join('\n', top.Select((f, i) =>
            {
                var user = _client.GetUser(f.UserId);
                var w = f.Weight >= 1000 ? $"{f.Weight / 1000.0:0.##}kg" : $"{f.Weight}g";
                return $"`#{i + 1}` **{f.FishName}** ({f.Rarity}) - {w} - {user?.ToString() ?? f.UserId.ToString()}";
            }));

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.gfish_lb_title))
                .WithDescription(desc);

            await Response().Embed(eb).SendAsync();
        }
    }
}
