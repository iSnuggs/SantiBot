#nullable disable
using SantiBot.Db.Models;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("RealEstate")]
    [Group("realestate")]
    public partial class RealEstateCommands : SantiModule<RealEstate.RealEstateService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Buy([Leftover] string propertyType)
        {
            var (success, message) = await _service.BuyPropertyAsync(ctx.Guild.Id, ctx.User.Id, propertyType?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Upgrade([Leftover] string propertyType)
        {
            var (success, message) = await _service.UpgradePropertyAsync(ctx.Guild.Id, ctx.User.Id, propertyType?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task List()
        {
            var props = await _service.GetPropertiesAsync(ctx.Guild.Id, ctx.User.Id);
            if (props.Count == 0)
            {
                await Response().Error("You don't own any properties! Use `.realestate buy <type>` to purchase one.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"🏠 {ctx.User.Username}'s Properties")
                .WithOkColor();

            foreach (var prop in props)
            {
                if (RealEstate.RealEstateService.Properties.TryGetValue(prop.PropertyType, out var info))
                {
                    var income = info.IncomePerHour * (long)Math.Pow(2, prop.UpgradeLevel);
                    eb.AddField(
                        $"{prop.PropertyType} (Lvl {prop.UpgradeLevel})",
                        $"Income: {income} 🥠/hr",
                        true);
                }
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Collect()
        {
            var (success, message, _) = await _service.CollectIncomeAsync(ctx.Guild.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Sell([Leftover] string propertyType)
        {
            var (success, message) = await _service.SellPropertyAsync(ctx.Guild.Id, ctx.User.Id, propertyType?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
