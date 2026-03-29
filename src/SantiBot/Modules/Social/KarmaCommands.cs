#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Karma")]
    [Group("karma")]
    public partial class KarmaCommands : SantiModule<KarmaService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Karma(IUser user = null)
        {
            user ??= ctx.User;
            var karma = await _service.GetKarmaAsync(ctx.Guild.Id, user.Id);
            var net = karma.Upvotes - karma.Downvotes;

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle("Karma Stats")
                .AddField("\u2B06\uFE0F Upvotes", karma.Upvotes.ToString("N0"), true)
                .AddField("\u2B07\uFE0F Downvotes", karma.Downvotes.ToString("N0"), true)
                .AddField("\u2728 Net Karma", net.ToString("N0"), true)
                .WithColor(net >= 0 ? Discord.Color.Green : Discord.Color.Red);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Karmaboard()
        {
            var top = await _service.GetKarmaLeaderboardAsync(ctx.Guild.Id);

            if (top.Count == 0)
            {
                await Response().Confirm("No karma data yet! React with \u2B06\uFE0F or \u2B07\uFE0F on messages.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\u2728 Karma Leaderboard");

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < top.Count; i++)
            {
                var k = top[i];
                var user = await ctx.Guild.GetUserAsync(k.UserId);
                var name = user?.ToString() ?? $"User {k.UserId}";
                var net = k.Upvotes - k.Downvotes;
                sb.AppendLine($"**#{i + 1}** {name} — {net:N0} karma (\u2B06{k.Upvotes} \u2B07{k.Downvotes})");
            }

            eb.WithDescription(sb.ToString());
            await Response().Embed(eb).SendAsync();
        }
    }
}
