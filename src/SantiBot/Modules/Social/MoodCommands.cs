#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Mood")]
    [Group("mood")]
    public partial class MoodCommands : SantiModule<MoodService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Mood(string emoji, [Leftover] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await Response().Error("Usage: `.mood <emoji> <message>`").SendAsync();
                return;
            }

            if (message.Length > 100)
            {
                await Response().Error("Mood message must be 100 characters or less!").SendAsync();
                return;
            }

            await _service.SetMoodAsync(ctx.Guild.Id, ctx.User.Id, emoji, message);
            await Response().Confirm($"Mood set! {emoji} {message} (expires in 24h)").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MoodClear()
        {
            await _service.ClearMoodAsync(ctx.Guild.Id, ctx.User.Id);
            await Response().Confirm("Mood cleared!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Moodboard()
        {
            var moods = await _service.GetMoodboardAsync(ctx.Guild.Id);

            if (moods.Count == 0)
            {
                await Response().Confirm("No active moods! Set one with `.mood <emoji> <message>`.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var m in moods)
            {
                var user = await ctx.Guild.GetUserAsync(m.UserId);
                var name = user?.ToString() ?? $"User {m.UserId}";
                sb.AppendLine($"{m.Emoji} **{name}** — {m.Message}");
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F3AD Server Mood Board")
                .WithDescription(sb.ToString())
                .WithFooter("Moods expire after 24 hours");

            await Response().Embed(eb).SendAsync();
        }
    }
}
