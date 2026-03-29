#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Friends")]
    [Group("friend")]
    public partial class FriendCommands : SantiModule<FriendshipService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FriendAdd(IUser target)
        {
            var (success, error) = await _service.SendRequestAsync(ctx.Guild.Id, ctx.User.Id, target.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }
            await Response().Confirm($"\U0001F91D Friend request sent to {target.Mention}!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FriendAccept(IUser from)
        {
            var (success, error) = await _service.AcceptRequestAsync(ctx.Guild.Id, ctx.User.Id, from.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }
            await Response().Confirm($"\U0001F389 You and {from.Mention} are now friends!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FriendDeny(IUser from)
        {
            var (success, error) = await _service.DenyRequestAsync(ctx.Guild.Id, ctx.User.Id, from.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }
            await Response().Confirm("Friend request denied.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FriendRemove(IUser target)
        {
            var removed = await _service.RemoveFriendAsync(ctx.Guild.Id, ctx.User.Id, target.Id);
            if (!removed)
            {
                await Response().Error("You're not friends with that person!").SendAsync();
                return;
            }
            await Response().Confirm($"Removed {target.Mention} from friends.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FriendList()
        {
            var friends = await _service.GetFriendsAsync(ctx.Guild.Id, ctx.User.Id);
            if (friends.Count == 0)
            {
                await Response().Confirm("You don't have any friends yet! Use `.friend add @user`.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var f in friends)
            {
                var friendId = f.User1Id == ctx.User.Id ? f.User2Id : f.User1Id;
                var user = await ctx.Guild.GetUserAsync(friendId);
                var name = user?.ToString() ?? $"User {friendId}";
                sb.AppendLine($"\u2022 {name} (Interactions: {f.InteractionCount})");
            }

            var eb = CreateEmbed()
                .WithTitle($"\U0001F46B {ctx.User}'s Friends ({friends.Count})")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FriendRequests()
        {
            var requests = await _service.GetPendingRequestsAsync(ctx.Guild.Id, ctx.User.Id);
            if (requests.Count == 0)
            {
                await Response().Confirm("No pending friend requests!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var r in requests)
            {
                var user = await ctx.Guild.GetUserAsync(r.User1Id);
                var name = user?.ToString() ?? $"User {r.User1Id}";
                sb.AppendLine($"\u2022 {name} — `.friend accept @{name}` or `.friend deny @{name}`");
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F4E8 Pending Friend Requests")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
