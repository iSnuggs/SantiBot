#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Marriage")]
    [Group("marriage")]
    public partial class MarriageCommands : SantiModule<MarriageService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Marry(IUser target)
        {
            var (success, error) = await _service.MarryAsync(ctx.Guild.Id, ctx.User.Id, target.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F48D Marriage!")
                .WithDescription($"{ctx.User.Mention} and {target.Mention} are now married!\nCost: 1000 \U0001F960")
                .WithColor(Discord.Color.Magenta);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Divorce()
        {
            var (success, error) = await _service.DivorceAsync(ctx.Guild.Id, ctx.User.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm("\U0001F494 You are now divorced. Cost: 500 \U0001F960").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Adopt(IUser child)
        {
            var (success, error) = await _service.AdoptAsync(ctx.Guild.Id, ctx.User.Id, child.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"\U0001F46A {ctx.User.Mention} adopted {child.Mention}!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Spouse()
        {
            var marriage = await _service.GetMarriageAsync(ctx.Guild.Id, ctx.User.Id);
            if (marriage is null)
            {
                await Response().Confirm("You're not married!").SendAsync();
                return;
            }

            var spouseId = marriage.User1Id == ctx.User.Id ? marriage.User2Id : marriage.User1Id;
            var spouse = await ctx.Guild.GetUserAsync(spouseId);
            var spouseName = spouse?.ToString() ?? $"User {spouseId}";

            var eb = CreateEmbed()
                .WithTitle("\U0001F48D Your Spouse")
                .WithDescription($"Married to **{spouseName}**")
                .AddField("Since", marriage.DateAdded?.ToString("MMM dd, yyyy") ?? "Unknown")
                .WithColor(Discord.Color.Magenta);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Family(IUser user = null)
        {
            user ??= ctx.User;
            var adoptions = await _service.GetFamilyAsync(ctx.Guild.Id, user.Id);
            var marriage = await _service.GetMarriageAsync(ctx.Guild.Id, user.Id);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{user}**'s Family Tree");
            sb.AppendLine("```");

            if (marriage is not null)
            {
                var spouseId = marriage.User1Id == user.Id ? marriage.User2Id : marriage.User1Id;
                var spouse = await ctx.Guild.GetUserAsync(spouseId);
                sb.AppendLine($"\U0001F48D Spouse: {spouse?.ToString() ?? $"User {spouseId}"}");
            }

            var parents = adoptions.Where(a => a.ChildId == user.Id).ToList();
            foreach (var p in parents)
            {
                var parent = await ctx.Guild.GetUserAsync(p.ParentId);
                sb.AppendLine($"\u2191 Parent: {parent?.ToString() ?? $"User {p.ParentId}"}");
            }

            var children = adoptions.Where(a => a.ParentId == user.Id).ToList();
            foreach (var c in children)
            {
                var child = await ctx.Guild.GetUserAsync(c.ChildId);
                sb.AppendLine($"  \u2514 Child: {child?.ToString() ?? $"User {c.ChildId}"}");
            }

            if (adoptions.Count == 0 && marriage is null)
                sb.AppendLine("No family connections yet!");

            sb.AppendLine("```");

            await Response().Confirm(sb.ToString()).SendAsync();
        }
    }
}
