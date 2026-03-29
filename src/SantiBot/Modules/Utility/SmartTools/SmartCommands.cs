#nullable disable
using SantiBot.Common;
using SantiBot.Modules.Utility.SmartTools;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Smart Tools")]
    [Group("smart")]
    public partial class SmartCommands : SantiModule<SmartService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Summarize(int count = 20)
        {
            if (count < 2 || count > 100)
            {
                await Response().Error("Please specify a number between 2 and 100.").SendAsync();
                return;
            }

            var messages = (await ctx.Channel.GetMessagesAsync(count + 1).FlattenAsync())
                .Where(m => !m.Author.IsBot && !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => m.Content)
                .Reverse()
                .ToList();

            if (messages.Count == 0)
            {
                await Response().Error("No messages found to summarize.").SendAsync();
                return;
            }

            var summary = _service.SummarizeMessages(messages, Math.Min(7, messages.Count));

            var eb = CreateEmbed()
                .WithTitle("Conversation Summary")
                .WithDescription(summary)
                .WithFooter($"Analyzed {messages.Count} messages")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Faq([Leftover] string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                await Response().Error("Please provide a question to search for.").SendAsync();
                return;
            }

            var result = _service.FindAnswer(ctx.Guild.Id, question);

            if (result is null)
            {
                await Response().Confirm("No matching FAQ found. Try `.smart faqlist` to see available FAQs.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("FAQ Match")
                .AddField("Question", result.Value.Question)
                .AddField("Answer", result.Value.Answer)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task FaqAdd(string question, [Leftover] string answer)
        {
            if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
            {
                await Response().Error("Please provide both a question and an answer.").SendAsync();
                return;
            }

            _service.AddFaq(ctx.Guild.Id, question, answer);
            await Response().Confirm($"FAQ added!\n**Q:** {question}\n**A:** {answer}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FaqList()
        {
            var faqs = _service.GetAllFaqs(ctx.Guild.Id);

            if (faqs.Count == 0)
            {
                await Response().Confirm("No FAQs have been added yet. Use `.smart faqadd \"question\" answer` to add one.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < faqs.Count; i++)
            {
                sb.AppendLine($"**{i + 1}.** {faqs[i].Question}");
                sb.AppendLine($"   {faqs[i].Answer}");
            }

            var eb = CreateEmbed()
                .WithTitle($"Server FAQs ({faqs.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task FaqRemove([Leftover] string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                await Response().Error("Please provide the question to remove.").SendAsync();
                return;
            }

            if (_service.RemoveFaq(ctx.Guild.Id, question))
                await Response().Confirm($"FAQ removed: {question}").SendAsync();
            else
                await Response().Error("FAQ not found. Use `.smart faqlist` to see existing FAQs.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Sentiment([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Please provide some text to analyze.").SendAsync();
                return;
            }

            var (score, label) = _service.AnalyzeSentiment(text);

            var emoji = label switch
            {
                "Very Positive" => "\U0001f929",
                "Positive" => "\U0001f60a",
                "Neutral" => "\U0001f610",
                "Negative" => "\U0001f61e",
                "Very Negative" => "\U0001f621",
                _ => "\U0001f914"
            };

            var bar = BuildSentimentBar(score);

            var eb = CreateEmbed()
                .WithTitle($"Sentiment Analysis {emoji}")
                .AddField("Text", text.Length > 200 ? text[..200] + "..." : text)
                .AddField("Score", $"`{score:+#;-#;0}` / 100", true)
                .AddField("Label", label, true)
                .AddField("Meter", bar)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        private static string BuildSentimentBar(int score)
        {
            // Normalize score from [-100, 100] to [0, 20]
            var position = (int)Math.Round((score + 100) / 10.0);
            position = Math.Clamp(position, 0, 20);

            var bar = new char[20];
            for (var i = 0; i < 20; i++)
                bar[i] = i < position ? '\u2588' : '\u2591';

            return $"`\u2212100` [{new string(bar)}] `+100`";
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Topics([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Please provide some text to analyze.").SendAsync();
                return;
            }

            var topics = _service.DetectTopics(text);

            if (topics.Count == 0)
            {
                await Response().Confirm("No specific topics detected in the text.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            var topicEmojis = new Dictionary<string, string>
            {
                ["Gaming"] = "\U0001f3ae", ["Music"] = "\U0001f3b5", ["Art"] = "\U0001f3a8",
                ["Tech"] = "\U0001f4bb", ["Sports"] = "\u26bd", ["Food"] = "\U0001f354",
                ["School"] = "\U0001f4da", ["Movies"] = "\U0001f3ac", ["Anime"] = "\u2728",
                ["Politics"] = "\U0001f3db\ufe0f", ["Memes"] = "\U0001f602"
            };

            foreach (var (topic, hits) in topics)
            {
                var emoji = topicEmojis.GetValueOrDefault(topic, "\U0001f4cc");
                var hitBar = new string('\u2588', Math.Min(hits, 15));
                sb.AppendLine($"{emoji} **{topic}** — {hits} keyword{(hits > 1 ? "s" : "")} `{hitBar}`");
            }

            var eb = CreateEmbed()
                .WithTitle("Topic Detection")
                .WithDescription(sb.ToString())
                .WithFooter("Based on keyword matching")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Greet()
        {
            var greeting = _service.GenerateGreeting();

            var eb = CreateEmbed()
                .WithDescription(greeting)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WritingCheck([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Please provide some text to check.").SendAsync();
                return;
            }

            var report = _service.CheckWriting(text);

            var eb = CreateEmbed()
                .WithTitle("Writing Check")
                .AddField("Stats",
                    $"Words: **{report.WordCount}** | " +
                    $"Sentences: **{report.SentenceCount}** | " +
                    $"Avg Word Length: **{report.AvgWordLength}**")
                .WithOkColor();

            if (report.TyposFixed > 0)
                eb.AddField($"Typos Fixed ({report.TyposFixed})",
                    report.CorrectedText.Length > 500 ? report.CorrectedText[..500] + "..." : report.CorrectedText);

            if (report.Issues.Count > 0)
            {
                var issueText = string.Join("\n", report.Issues.Select(i => $"- {i}"));
                if (issueText.Length > 1000)
                    issueText = issueText[..1000] + "...";
                eb.AddField($"Issues ({report.Issues.Count})", issueText);
            }
            else
            {
                eb.AddField("Result", "No issues found! Your writing looks good.");
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task NameGen([Leftover] string type = null)
        {
            type = type?.ToLowerInvariant()?.Trim();

            if (type is not null && !SmartService.NameTypes.Contains(type))
            {
                await Response().Error($"Unknown type. Available: {string.Join(", ", SmartService.NameTypes)}").SendAsync();
                return;
            }

            // Generate a few names to give options
            var names = new List<string>();
            for (var i = 0; i < 5; i++)
                names.Add(_service.GenerateName(type ?? "fantasy"));

            var displayType = type ?? "fantasy";
            var typeEmoji = displayType switch
            {
                "character" => "\U0001f9d1",
                "band" => "\U0001f3b8",
                "superhero" => "\U0001f9b8",
                "fantasy" => "\u2694\ufe0f",
                "elf" => "\U0001f9dd",
                "dwarf" => "\u26cf\ufe0f",
                "orc" => "\U0001f479",
                _ => "\u2728"
            };

            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < names.Count; i++)
                sb.AppendLine($"**{i + 1}.** {names[i]}");

            var eb = CreateEmbed()
                .WithTitle($"{typeEmoji} {char.ToUpper(displayType[0])}{displayType[1..]} Name Generator")
                .WithDescription(sb.ToString())
                .WithFooter("Run again for more names!")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }
}
