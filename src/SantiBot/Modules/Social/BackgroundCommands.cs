#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Backgrounds")]
    [Group("bg")]
    public partial class BackgroundCommands : SantiModule<BackgroundShopService>
    {
        private readonly ProfileService _profiles;

        public BackgroundCommands(ProfileService profiles)
        {
            _profiles = profiles;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bgshop()
        {
            var items = _service.GetShopItems();
            var eb = CreateEmbed()
                .WithTitle("\U0001F3A8 Background Shop")
                .WithDescription("Buy profile color themes with \U0001F960!");

            foreach (var item in items)
            {
                eb.AddField($"{item.Name} (`{item.Id}`)", $"Color: `{item.Hex}` | Price: {item.Price} \U0001F960", true);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bgbuy(string id)
        {
            var bg = _service.GetBackground(id);
            if (bg is null)
            {
                await Response().Error("Background not found! Use `.bgshop` to see available ones.").SendAsync();
                return;
            }

            var owns = await _service.OwnsBackgroundAsync(ctx.User.Id, bg.Value.Id);
            if (owns)
            {
                await Response().Error("You already own this background!").SendAsync();
                return;
            }

            var bought = await _service.BuyBackgroundAsync(ctx.User.Id, bg.Value.Id, bg.Value.Price);
            if (!bought)
            {
                await Response().Error($"You need {bg.Value.Price} \U0001F960 to buy this!").SendAsync();
                return;
            }

            await Response().Confirm($"Purchased **{bg.Value.Name}** background! Use `.bgset {bg.Value.Id}` to equip it.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bgset(string id)
        {
            var bg = _service.GetBackground(id);
            if (bg is null)
            {
                await Response().Error("Background not found!").SendAsync();
                return;
            }

            if (id != "default")
            {
                var owns = await _service.OwnsBackgroundAsync(ctx.User.Id, bg.Value.Id);
                if (!owns)
                {
                    await Response().Error("You don't own this background! Buy it first with `.bgbuy`.").SendAsync();
                    return;
                }
            }

            await _profiles.SetBackgroundAsync(ctx.Guild.Id, ctx.User.Id, bg.Value.Id, bg.Value.Name, bg.Value.Hex);
            await Response().Confirm($"Profile background set to **{bg.Value.Name}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bglist()
        {
            var owned = await _service.GetOwnedAsync(ctx.User.Id);
            var shop = _service.GetShopItems();

            if (owned.Count == 0)
            {
                await Response().Confirm("You don't own any backgrounds yet! Check `.bgshop`.").SendAsync();
                return;
            }

            var descriptions = owned.Select(o =>
            {
                var info = shop.FirstOrDefault(s => s.Id == o.BackgroundId);
                return info != default ? $"**{info.Name}** (`{info.Id}`) - {info.Hex}" : $"`{o.BackgroundId}`";
            });

            var eb = CreateEmbed()
                .WithTitle("\U0001F3A8 Your Backgrounds")
                .WithDescription(string.Join("\n", descriptions));

            await Response().Embed(eb).SendAsync();
        }
    }
}
