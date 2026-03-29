#nullable disable
using SantiBot.Modules.Gambling.Business;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Business")]
    [Group("biz")]
    public partial class BusinessCommands : SantiModule<BusinessService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Create(string name, [Leftover] string type)
        {
            var (success, message) = await _service.CreateBusinessAsync(ctx.Guild.Id, ctx.User.Id, name, type?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Hire(IUser user)
        {
            var (success, message) = await _service.HireAsync(ctx.Guild.Id, ctx.User.Id, user.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Fire(IUser user)
        {
            var (success, message) = await _service.FireAsync(ctx.Guild.Id, ctx.User.Id, user.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Info()
        {
            var biz = await _service.GetBusinessInfoAsync(ctx.Guild.Id, ctx.User.Id);
            if (biz is null)
            {
                await Response().Error("You don't own a business!").SendAsync();
                return;
            }

            var empCount = await _service.GetEmployeeCountAsync(biz.Id);
            var info = BusinessService.BusinessTypes[biz.BusinessType];

            var eb = CreateEmbed()
                .WithTitle($"🏢 {biz.Name}")
                .AddField("Type", biz.BusinessType, true)
                .AddField("Employees", $"{empCount}/{info.MaxEmployees}", true)
                .AddField("Revenue", $"{biz.Revenue} 🥠", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task List()
        {
            var businesses = await _service.ListBusinessesAsync(ctx.Guild.Id);
            if (businesses.Count == 0)
            {
                await Response().Error("No businesses in this server!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("🏢 Server Businesses")
                .WithOkColor();

            foreach (var b in businesses.Take(10))
                eb.AddField(b.Name, $"Type: {b.BusinessType}\nOwner: <@{b.OwnerId}>", true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Work()
        {
            var (success, message) = await _service.WorkAtBusinessAsync(ctx.Guild.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
