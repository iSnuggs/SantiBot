#nullable disable
namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Proactive Santi")]
    [Group("santi")]
    public partial class ProactiveSantiCommands : SantiModule<OpenClaw.ProactiveSantiService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Schedule(string postType, ITextChannel channel, [Leftover] string schedule = "0 9 * * *")
        {
            if (!OpenClaw.ProactiveSantiService.PostTypes.ContainsKey(postType) && postType != "Custom")
            {
                var types = string.Join(", ", OpenClaw.ProactiveSantiService.PostTypes.Keys.Select(k => $"`{k}`"));
                await Response().Error($"Unknown type! Available: {types}, `Custom`").SendAsync();
                return;
            }

            var post = await _service.AddScheduledPostAsync(ctx.Guild.Id, channel.Id, postType, schedule, ctx.User.Id);
            var typeDef = OpenClaw.ProactiveSantiService.PostTypes.TryGetValue(postType, out var td) ? td : ("Custom", "🐾", "");

            await Response().Confirm(
                $"{typeDef.Item2} **{typeDef.Item1}** scheduled!\n" +
                $"Channel: {channel.Mention}\n" +
                $"Schedule: `{schedule}`\n" +
                $"ID: {post.Id}\n\n" +
                $"Test it: `.santi postnow {postType}`").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ScheduleCustom(ITextChannel channel, [Leftover] string prompt)
        {
            var post = await _service.AddScheduledPostAsync(ctx.Guild.Id, channel.Id, "Custom", "0 9 * * *", ctx.User.Id, prompt);
            await Response().Confirm(
                $"🐾 **Custom scheduled post** created!\n" +
                $"Channel: {channel.Mention}\n" +
                $"Prompt: *{prompt}*\n" +
                $"ID: {post.Id}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Posts()
        {
            var posts = await _service.GetScheduledPostsAsync(ctx.Guild.Id);
            if (posts.Count == 0)
            {
                await Response().Confirm("No scheduled Santi posts! Admins: `.santi schedule <type> #channel`").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var p in posts)
            {
                var typeDef = OpenClaw.ProactiveSantiService.PostTypes.TryGetValue(p.PostType, out var td)
                    ? td : ("Custom", "🐾", "");
                var status = p.IsEnabled ? "🟢" : "🔴";
                sb.AppendLine($"{status} **{typeDef.Item2} {typeDef.Item1}** (ID: {p.Id})");
                sb.AppendLine($"  Channel: <#{p.ChannelId}> | Schedule: `{p.CronSchedule}`");
                if (p.LastPostedAt.HasValue)
                    sb.AppendLine($"  Last posted: <t:{new DateTimeOffset(p.LastPostedAt.Value).ToUnixTimeSeconds()}:R>");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithTitle($"🐾 Scheduled Santi Posts ({posts.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task PostNow([Leftover] string postType = "DailyFact")
        {
            using var typing = ctx.Channel.EnterTypingState();
            var (success, response) = await _service.PostNowAsync(ctx.Guild.Id, ctx.Channel.Id, postType);

            if (!success)
                await Response().Error($"Failed: {response}").SendAsync();
            // Success = the post was already sent by the service
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task PostRemove(int postId)
        {
            var success = await _service.RemoveScheduledPostAsync(ctx.Guild.Id, postId);
            if (success)
                await Response().Confirm("Scheduled post removed!").SendAsync();
            else
                await Response().Error("Post not found!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task PostToggle(int postId)
        {
            var success = await _service.TogglePostAsync(ctx.Guild.Id, postId);
            if (success)
                await Response().Confirm("Post toggled!").SendAsync();
            else
                await Response().Error("Post not found!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PostTypes()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("**Available post types for scheduling:**\n");
            foreach (var (key, (name, emoji, prompt)) in OpenClaw.ProactiveSantiService.PostTypes)
            {
                sb.AppendLine($"{emoji} **{name}** (`{key}`)");
                sb.AppendLine($"  *{prompt[..System.Math.Min(80, prompt.Length)]}...*\n");
            }
            sb.AppendLine("**Custom:** `.santi schedulecustom #channel Your custom prompt here`");

            var eb = CreateEmbed()
                .WithTitle("🐾 Santi Post Types")
                .WithDescription(sb.ToString())
                .WithFooter("Schedule: .santi schedule <type> #channel [cron]")
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }
    }
}
