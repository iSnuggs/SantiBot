#nullable disable
using SantiBot.Modules.Games.MiniGames;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Mini Games")]
    [Group("mg")]
    public partial class MiniGameCommands : SantiModule<MiniGameService>
    {
        // ─── WORDLE ──────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Wordle()
        {
            var game = _service.StartWordle(ctx.Channel.Id);
            if (game is null)
            {
                await Response()
                    .Error("A Wordle game is already running in this channel! Use `.mg wordleguess <word>` to play or `.mg wordleend` to stop.")
                    .SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Wordle")
                .WithDescription(
                    "Guess the 5-letter word! You have **6** attempts.\n\n" +
                    "\ud83d\udfe9 = Correct letter, correct position\n" +
                    "\ud83d\udfe8 = Correct letter, wrong position\n" +
                    "\u2b1c = Letter not in word\n\n" +
                    $"Use `{prefix}mg wordleguess <word>` to guess.");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WordleGuess([Leftover] string guess)
        {
            var (game, result) = _service.GuessWordle(ctx.Channel.Id, guess);
            if (game is null)
            {
                await Response().Error("No Wordle game is running. Start one with `.mg wordle`!").SendAsync();
                return;
            }

            if (result == "INVALID_LENGTH")
            {
                await Response().Error("Your guess must be exactly 5 letters!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("Wordle");

            // Build the full grid of all guesses
            var gridLines = new List<string>();
            for (var i = 0; i < game.Guesses.Count; i++)
            {
                var g = game.Guesses[i];
                var r = i == game.Guesses.Count - 1
                    ? result
                    : MiniGameService.FormatWordleResult(g, EvaluateForDisplay(game.TargetWord, g)).Split(' ')[0] != null
                        ? ReEvaluate(game.TargetWord, g)
                        : result;

                // For past guesses, re-evaluate
                if (i < game.Guesses.Count - 1)
                    r = ReEvaluate(game.TargetWord, g);

                gridLines.Add(MiniGameService.FormatWordleResult(g, r));
            }

            var desc = string.Join('\n', gridLines);

            if (game.IsOver && game.Won)
            {
                eb.WithColor(new Color(0x57F287))
                    .WithDescription(desc + $"\n\nYou got it in **{game.Guesses.Count}** guess{(game.Guesses.Count > 1 ? "es" : "")}!");
            }
            else if (game.IsOver)
            {
                eb.WithColor(new Color(0xED4245))
                    .WithDescription(desc + $"\n\nGame over! The word was **{game.TargetWord.ToUpper()}**.");
            }
            else
            {
                eb.WithOkColor()
                    .WithDescription(desc + $"\n\n{game.MaxAttempts - game.Guesses.Count} attempts remaining.");
            }

            await Response().Embed(eb).SendAsync();
        }

        private static string ReEvaluate(string target, string guess)
        {
            var result = new char[5];
            var targetChars = target.ToCharArray();
            var used = new bool[5];

            for (var i = 0; i < 5; i++)
            {
                if (guess[i] == targetChars[i])
                {
                    result[i] = 'G';
                    used[i] = true;
                }
            }

            for (var i = 0; i < 5; i++)
            {
                if (result[i] == 'G') continue;
                var found = false;
                for (var j = 0; j < 5; j++)
                {
                    if (!used[j] && guess[i] == targetChars[j])
                    {
                        result[i] = 'Y';
                        used[j] = true;
                        found = true;
                        break;
                    }
                }
                if (!found) result[i] = 'B';
            }

            return new string(result);
        }

        private static string EvaluateForDisplay(string target, string guess)
            => ReEvaluate(target, guess);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WordleEnd()
        {
            var game = _service.GetWordleGame(ctx.Channel.Id);
            if (game is null)
            {
                await Response().Error("No Wordle game is running in this channel.").SendAsync();
                return;
            }

            _service.EndWordle(ctx.Channel.Id);
            await Response().Confirm($"Wordle game ended. The word was **{game.TargetWord.ToUpper()}**.").SendAsync();
        }

        // ─── MINESWEEPER ────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Minesweeper([Leftover] string difficulty = "easy")
        {
            var valid = new[] { "easy", "medium", "hard" };
            if (!valid.Contains(difficulty.ToLowerInvariant()))
            {
                await Response().Error("Difficulty must be `easy`, `medium`, or `hard`.").SendAsync();
                return;
            }

            var grid = _service.GenerateMinesweeper(difficulty);
            await ctx.Channel.SendMessageAsync(grid);
        }

        // ─── NUMBER GUESS ───────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GuessNumber()
        {
            var game = _service.StartNumberGuess(ctx.Channel.Id);
            if (game is null)
            {
                await Response()
                    .Error($"A number guessing game is already running! Use `{prefix}mg gn <number>` to guess or `{prefix}mg gnend` to stop.")
                    .SendAsync();
                return;
            }

            await Response()
                .Confirm(
                    $"I'm thinking of a number between **1** and **1000**.\n" +
                    $"Use `{prefix}mg gn <number>` to guess!")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Gn(int guess)
        {
            var (game, hint) = _service.GuessNumber(ctx.Channel.Id, guess);
            if (game is null)
            {
                await Response().Error($"No guessing game is running. Start one with `{prefix}mg guessnumber`!").SendAsync();
                return;
            }

            if (hint == "CORRECT")
            {
                await Response()
                    .Confirm($"**{ctx.User.Username}** guessed it! The number was **{game.TargetNumber}** in **{game.Attempts}** attempts!")
                    .SendAsync();
            }
            else
            {
                var direction = hint == "HIGHER" ? "higher" : "lower";
                await Response()
                    .Confirm($"**{guess}** is too {(hint == "HIGHER" ? "low" : "high")}! Go **{direction}**. (Attempt #{game.Attempts})")
                    .SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GnEnd()
        {
            var game = _service.GetGuessGame(ctx.Channel.Id);
            if (game is null)
            {
                await Response().Error("No guessing game is running in this channel.").SendAsync();
                return;
            }

            _service.EndGuessGame(ctx.Channel.Id);
            await Response().Confirm($"Game ended. The number was **{game.TargetNumber}**.").SendAsync();
        }

        // ─── WORD SCRAMBLE ──────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Scramble()
        {
            var game = _service.StartScramble(ctx.Channel.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Word Scramble")
                .WithDescription(
                    $"Unscramble this word:\n\n" +
                    $"## `{game.ScrambledWord.ToUpper()}`\n\n" +
                    $"Use `{prefix}mg unscramble <word>` to answer!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Unscramble([Leftover] string answer)
        {
            var (correct, game) = _service.CheckScramble(ctx.Channel.Id, answer);
            if (game is null)
            {
                await Response().Error($"No scramble game is running. Start one with `{prefix}mg scramble`!").SendAsync();
                return;
            }

            if (correct)
            {
                await Response()
                    .Confirm($"**{ctx.User.Username}** got it! The word was **{game.OriginalWord.ToUpper()}**!")
                    .SendAsync();
            }
            else
            {
                await Response()
                    .Error($"That's not right! The scrambled word is: `{game.ScrambledWord.ToUpper()}`")
                    .SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ScrambleEnd()
        {
            var game = _service.GetScrambleGame(ctx.Channel.Id);
            if (game is null)
            {
                await Response().Error("No scramble game is running in this channel.").SendAsync();
                return;
            }

            _service.EndScramble(ctx.Channel.Id);
            await Response().Confirm($"Scramble ended. The word was **{game.OriginalWord.ToUpper()}**.").SendAsync();
        }

        // ─── GEOGRAPHY QUIZ ────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GeoQuiz()
        {
            var (country, capital) = _service.GetGeoQuestion();

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Geography Quiz")
                .WithDescription($"What is the capital of **{country}**?")
                .WithFooter($"Answer with: {prefix}mg geoanswer <capital>");

            await Response().Embed(eb).SendAsync();

            // Store the answer temporarily for checking
            _geoAnswers[ctx.Channel.Id] = capital;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, string> _geoAnswers = new();

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GeoAnswer([Leftover] string answer)
        {
            if (!_geoAnswers.TryRemove(ctx.Channel.Id, out var correctAnswer))
            {
                await Response().Error($"No geography quiz is active. Start one with `{prefix}mg geoquiz`!").SendAsync();
                return;
            }

            if (MiniGameService.CheckGeoAnswer(answer, correctAnswer))
            {
                await Response()
                    .Confirm($"Correct! **{correctAnswer}** is right! Well done, **{ctx.User.Username}**!")
                    .SendAsync();
            }
            else
            {
                await Response()
                    .Error($"Wrong! The correct answer was **{correctAnswer}**.")
                    .SendAsync();
            }
        }

        // ─── MATH RACE ─────────────────────

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, int> _mathAnswers = new();

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MathRace()
        {
            var (problem, answer) = _service.GenerateMathProblem();
            _mathAnswers[ctx.Channel.Id] = answer;

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Math Race!")
                .WithDescription($"First to solve this wins!\n\n## {problem} = ?\n\nUse `{prefix}mg mathanswer <number>` to answer!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MathAnswer(int answer)
        {
            if (!_mathAnswers.TryRemove(ctx.Channel.Id, out var correctAnswer))
            {
                await Response().Error($"No math race is active. Start one with `{prefix}mg mathrace`!").SendAsync();
                return;
            }

            if (answer == correctAnswer)
            {
                await Response()
                    .Confirm($"**{ctx.User.Username}** wins! The answer was **{correctAnswer}**!")
                    .SendAsync();
            }
            else
            {
                // Put the answer back so others can try
                _mathAnswers.TryAdd(ctx.Channel.Id, correctAnswer);
                await Response().Error($"**{answer}** is wrong! Keep trying!").SendAsync();
            }
        }

        // ─── EMOJI QUIZ ────────────────────

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, string> _emojiAnswers = new();

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EmojiQuiz()
        {
            var (emojis, answer) = _service.GetEmojiQuiz();
            _emojiAnswers[ctx.Channel.Id] = answer;

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Emoji Quiz")
                .WithDescription(
                    $"What movie or show do these emojis represent?\n\n" +
                    $"## {emojis}\n\n" +
                    $"Use `{prefix}mg emojianswer <title>` to answer!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EmojiAnswer([Leftover] string answer)
        {
            if (!_emojiAnswers.TryRemove(ctx.Channel.Id, out var correctAnswer))
            {
                await Response().Error($"No emoji quiz is active. Start one with `{prefix}mg emojiquiz`!").SendAsync();
                return;
            }

            if (MiniGameService.CheckEmojiQuizAnswer(answer, correctAnswer))
            {
                await Response()
                    .Confirm($"Correct! **{correctAnswer}** is right! Great job, **{ctx.User.Username}**!")
                    .SendAsync();
            }
            else
            {
                await Response()
                    .Error($"Wrong! The answer was **{correctAnswer}**.")
                    .SendAsync();
            }
        }

        // ─── MEMORY GAME ───────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MemoryGame()
        {
            var game = _service.StartMemoryGame(ctx.Channel.Id);

            var sequence = string.Join(" ", game.Sequence);
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Memory Game - Level 1")
                .WithDescription(
                    $"Memorize this sequence of emojis!\n\n" +
                    $"## {sequence}\n\n" +
                    $"Repeat them back with `{prefix}mg memoryanswer <emojis>`\n" +
                    $"(Copy and paste the emojis in the same order, separated by spaces)");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MemoryAnswer([Leftover] string answer)
        {
            var (correct, game) = _service.CheckMemoryAnswer(ctx.Channel.Id, answer);
            if (game is null)
            {
                await Response().Error($"No memory game is running. Start one with `{prefix}mg memorygame`!").SendAsync();
                return;
            }

            if (correct)
            {
                var sequence = string.Join(" ", game.Sequence);
                var eb = CreateEmbed()
                    .WithColor(new Color(0x57F287))
                    .WithTitle($"Memory Game - Level {game.Level}")
                    .WithDescription(
                        $"Correct! Your score: **{game.Score}**\n\n" +
                        $"Next sequence:\n\n" +
                        $"## {sequence}\n\n" +
                        $"Repeat them back with `{prefix}mg memoryanswer <emojis>`");

                await Response().Embed(eb).SendAsync();
            }
            else
            {
                await Response()
                    .Confirm($"Wrong sequence! Game over. Your final score: **{game.Score}** level{(game.Score != 1 ? "s" : "")} completed.")
                    .SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MemoryEnd()
        {
            var game = _service.GetMemoryGame(ctx.Channel.Id);
            if (game is null)
            {
                await Response().Error("No memory game is running in this channel.").SendAsync();
                return;
            }

            _service.EndMemoryGame(ctx.Channel.Id);
            await Response().Confirm($"Memory game ended. Final score: **{game.Score}** levels.").SendAsync();
        }
    }
}
