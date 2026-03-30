#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Marriage")]
    [Group("marriage")]
    public partial class MarriageCommands : SantiModule<MarriageService>
    {
        // Pending proposals: targetUserId → (proposerId, guildId, expiresAt)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, (ulong ProposerId, ulong GuildId, DateTime ExpiresAt)> _pendingProposals = new();

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Marry(IUser target)
        {
            if (target.Id == ctx.User.Id)
            {
                await Response().Error("You can't marry yourself!").SendAsync();
                return;
            }

            // Check if already married
            var existing = await _service.GetMarriageAsync(ctx.Guild.Id, ctx.User.Id);
            if (existing is not null)
            {
                await Response().Error("You're already married! Divorce first with `.marriage divorce`").SendAsync();
                return;
            }

            // Create proposal — target must accept
            _pendingProposals[target.Id] = (ctx.User.Id, ctx.Guild.Id, DateTime.UtcNow.AddMinutes(5));

            var eb = CreateEmbed()
                .WithTitle("💍 Marriage Proposal!")
                .WithDescription(
                    $"{ctx.User.Mention} has proposed to {target.Mention}!\n\n" +
                    $"{target.Mention}, type `.marriage accept` within 5 minutes to accept!\n" +
                    $"Or `.marriage decline` to decline.\n\n" +
                    $"Cost: 1000 🥠 (split between both)")
                .WithColor(Discord.Color.Magenta);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Accept()
        {
            if (!_pendingProposals.TryRemove(ctx.User.Id, out var proposal))
            {
                await Response().Error("You don't have any pending proposals!").SendAsync();
                return;
            }

            if (proposal.GuildId != ctx.Guild.Id)
            {
                await Response().Error("That proposal was for a different server!").SendAsync();
                return;
            }

            if (DateTime.UtcNow > proposal.ExpiresAt)
            {
                await Response().Error("That proposal has expired!").SendAsync();
                return;
            }

            var (success, error) = await _service.MarryAsync(ctx.Guild.Id, proposal.ProposerId, ctx.User.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            // Award achievements
            AchievementService.Award(ctx.Guild.Id, proposal.ProposerId, "married");
            AchievementService.Award(ctx.Guild.Id, ctx.User.Id, "married");

            var eb = CreateEmbed()
                .WithTitle("💍 Married!")
                .WithDescription($"<@{proposal.ProposerId}> and {ctx.User.Mention} are now married! 🎉\nCongratulations!")
                .WithColor(Discord.Color.Magenta);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Decline()
        {
            if (!_pendingProposals.TryRemove(ctx.User.Id, out var proposal))
            {
                await Response().Error("You don't have any pending proposals!").SendAsync();
                return;
            }

            await Response().Confirm($"💔 {ctx.User.Mention} declined <@{proposal.ProposerId}>'s proposal.").SendAsync();
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
