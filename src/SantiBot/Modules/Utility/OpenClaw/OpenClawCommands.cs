#nullable disable
using SantiBot.Modules.Utility.OpenClaw;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("OpenClaw AI")]
    [Group("oc")]
    public partial class OpenClawCommands : SantiModule<OpenClawService>
    {
        /// <summary>Ask Claude anything — maintains conversation per user</summary>
        [Cmd]
        public async Task Ask([Leftover] string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                await Response().Error("Ask me something! Example: `.oc ask What's the best programming language?`").SendAsync();
                return;
            }

            // Show typing indicator while Claude thinks
            using var typing = ctx.Channel.EnterTypingState();

            var (success, response) = await _service.ChatAsync(ctx.User.Id, question);

            if (success)
            {
                var eb = CreateEmbed()
                    .WithAuthor($"{ctx.User.Username} asked Claude", ctx.User.GetAvatarUrl())
                    .WithDescription(response)
                    .WithFooter("Powered by OpenClaw + Claude | .oc reset to start fresh")
                    .WithOkColor();
                await Response().Embed(eb).SendAsync();
            }
            else
            {
                await Response().Error(response).SendAsync();
            }
        }

        /// <summary>Reset your conversation — Claude forgets everything</summary>
        [Cmd]
        public async Task Reset()
        {
            _service.ResetSession(ctx.User.Id);
            await Response().Confirm("🔄 Conversation reset! Claude has forgotten our previous chat. Ask me something new!").SendAsync();
        }

        /// <summary>Check if OpenClaw is online and responding</summary>
        [Cmd]
        public async Task Status()
        {
            using var typing = ctx.Channel.EnterTypingState();
            var online = await _service.IsOnlineAsync();

            var eb = CreateEmbed()
                .WithTitle("🦞 OpenClaw Status")
                .AddField("Gateway", online ? "🟢 Online" : "🔴 Offline", true)
                .AddField("Host", $"{Environment.GetEnvironmentVariable("OPENCLAW_SSH_HOST") ?? "configured"}:18789", true)
                .AddField("Model", "Claude Sonnet 4.6", true)
                .AddField("Features", "Multi-turn chat, web search, file access", false)
                .WithFooter("Use .oc ask <question> to chat with Claude")
                .WithColor(online ? Discord.Color.Green : Discord.Color.Red);
            await Response().Embed(eb).SendAsync();
        }

        /// <summary>Ask a quick one-off question with no session memory</summary>
        [Cmd]
        public async Task Quick([Leftover] string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                await Response().Error("Ask something! Example: `.oc quick What year was Discord founded?`").SendAsync();
                return;
            }

            using var typing = ctx.Channel.EnterTypingState();
            var (success, response) = await _service.QuickAskAsync(question);

            if (success)
                await Response().Confirm($"🦞 {response}").SendAsync();
            else
                await Response().Error(response).SendAsync();
        }

        /// <summary>Show help for OpenClaw commands</summary>
        [Cmd]
        public async Task Info()
        {
            var eb = CreateEmbed()
                .WithTitle("🦞 OpenClaw AI — Built into SantiBot")
                .WithDescription(
                    "Chat with Claude AI directly in Discord! OpenClaw runs on our local server " +
                    "and Claude responds through SantiBot — no second bot needed.\n\n" +
                    "**How it works:**\n" +
                    "You type → SantiBot forwards to OpenClaw → Claude responds → SantiBot posts the answer\n\n" +
                    "Each user gets their own conversation memory. Claude remembers what you've discussed " +
                    "until you reset with `.oc reset`.")
                .AddField("Commands", """
                    `.oc ask <question>` — Ask Claude (remembers conversation)
                    `.oc quick <question>` — Quick one-off question (no memory)
                    `.oc reset` — Reset your conversation
                    `.oc status` — Check if OpenClaw is online
                    `.oc info` — This help message
                    """, false)
                .AddField("Examples", """
                    `.oc ask Explain quantum computing like I'm 5`
                    `.oc ask Write me a Python script to sort a list`
                    `.oc ask What's the weather like in New York?`
                    `.oc quick Who won the 2024 World Series?`
                    """, false)
                .WithFooter("Powered by OpenClaw + Claude Sonnet 4.6")
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }
    }
}
