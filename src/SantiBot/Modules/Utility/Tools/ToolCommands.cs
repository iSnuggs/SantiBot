#nullable disable
using System.Globalization;
using System.Text.RegularExpressions;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Tools")]
    [Group("tools")]
    public partial class ToolCommands : SantiModule
    {
        private static readonly SantiRandom _rng = new();

        // ═══════════════════════════════════════════
        //  UNIT CONVERTER
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Convert([Leftover] string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                await Response().Confirm("**Usage:** `.tools convert <value> <from> to <to>`\n" +
                    "**Examples:**\n" +
                    "`.tools convert 100 f to c` — Fahrenheit to Celsius\n" +
                    "`.tools convert 5 km to mi` — Kilometers to miles\n" +
                    "`.tools convert 10 kg to lb` — Kilograms to pounds\n" +
                    "`.tools convert 1 gal to l` — Gallons to liters\n\n" +
                    "**Supported:** f/c, km/mi/m/ft/in/cm/mm/yd, kg/lb/oz/g/st, l/gal/qt/pt/cup/ml/fl oz, mph/kph/mps").SendAsync();
                return;
            }

            var match = Regex.Match(input.Trim(), @"^([\d.]+)\s*(\w+)\s+to\s+(\w+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                await Response().Error("Format: `.tools convert <value> <from> to <to>`").SendAsync();
                return;
            }

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                await Response().Error("Invalid number!").SendAsync();
                return;
            }

            var from = match.Groups[2].Value.ToLower().Replace(".", "");
            var to = match.Groups[3].Value.ToLower().Replace(".", "");

            var (result, fromUnit, toUnit) = ConvertUnit(value, from, to);
            if (result is null)
            {
                await Response().Error("Unknown conversion! Use `.tools convert` to see supported units.").SendAsync();
                return;
            }

            await Response().Confirm($"🔄 **{value:G} {fromUnit}** = **{result:G} {toUnit}**").SendAsync();
        }

        private static (double? Result, string From, string To) ConvertUnit(double val, string from, string to)
        {
            // Temperature
            if (from is "f" or "fahrenheit" && to is "c" or "celsius")
                return ((val - 32) * 5 / 9, "°F", "°C");
            if (from is "c" or "celsius" && to is "f" or "fahrenheit")
                return (val * 9 / 5 + 32, "°C", "°F");
            if (from is "c" or "celsius" && to is "k" or "kelvin")
                return (val + 273.15, "°C", "K");
            if (from is "k" or "kelvin" && to is "c" or "celsius")
                return (val - 273.15, "K", "°C");
            if (from is "f" or "fahrenheit" && to is "k" or "kelvin")
                return ((val - 32) * 5 / 9 + 273.15, "°F", "K");
            if (from is "k" or "kelvin" && to is "f" or "fahrenheit")
                return ((val - 273.15) * 9 / 5 + 32, "K", "°F");

            // Distance
            var distLookup = new Dictionary<string, double>
            {
                ["km"] = 1000, ["m"] = 1, ["cm"] = 0.01, ["mm"] = 0.001,
                ["mi"] = 1609.344, ["yd"] = 0.9144, ["ft"] = 0.3048, ["in"] = 0.0254,
                ["nm"] = 1852,
            };
            if (distLookup.TryGetValue(from, out var fromDist) && distLookup.TryGetValue(to, out var toDist))
                return (val * fromDist / toDist, from, to);

            // Weight
            var weightLookup = new Dictionary<string, double>
            {
                ["kg"] = 1, ["g"] = 0.001, ["mg"] = 0.000001,
                ["lb"] = 0.453592, ["oz"] = 0.0283495, ["st"] = 6.35029, ["t"] = 1000,
            };
            if (weightLookup.TryGetValue(from, out var fromW) && weightLookup.TryGetValue(to, out var toW))
                return (val * fromW / toW, from, to);

            // Volume
            var volLookup = new Dictionary<string, double>
            {
                ["l"] = 1, ["ml"] = 0.001, ["gal"] = 3.78541,
                ["qt"] = 0.946353, ["pt"] = 0.473176, ["cup"] = 0.236588,
                ["floz"] = 0.0295735, ["tbsp"] = 0.0147868, ["tsp"] = 0.00492892,
            };
            if (volLookup.TryGetValue(from, out var fromV) && volLookup.TryGetValue(to, out var toV))
                return (val * fromV / toV, from, to);

            // Speed
            var speedLookup = new Dictionary<string, double>
            {
                ["mph"] = 0.44704, ["kph"] = 0.277778, ["mps"] = 1, ["knots"] = 0.514444,
            };
            if (speedLookup.TryGetValue(from, out var fromS) && speedLookup.TryGetValue(to, out var toS))
                return (val * fromS / toS, from, to);

            // Data
            var dataLookup = new Dictionary<string, double>
            {
                ["b"] = 1, ["kb"] = 1024, ["mb"] = 1048576,
                ["gb"] = 1073741824, ["tb"] = 1099511627776,
            };
            if (dataLookup.TryGetValue(from, out var fromD) && dataLookup.TryGetValue(to, out var toD))
                return (val * fromD / toD, from, to);

            return (null, from, to);
        }

        // ═══════════════════════════════════════════
        //  TIMESTAMP GENERATOR
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Timestamp([Leftover] string input = null)
        {
            DateTimeOffset dt;
            if (string.IsNullOrWhiteSpace(input))
                dt = DateTimeOffset.UtcNow;
            else if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                dt = parsed;
            else if (input.StartsWith("+") && TimeSpan.TryParse(input[1..], out var offset))
                dt = DateTimeOffset.UtcNow + offset;
            else
            {
                await Response().Error("Couldn't parse that! Try: `2026-12-25 18:00`, `+1:30:00`, or leave empty for now.").SendAsync();
                return;
            }

            var unix = dt.ToUnixTimeSeconds();
            var eb = CreateEmbed()
                .WithTitle("🕐 Discord Timestamps")
                .AddField("Short Time", $"`<t:{unix}:t>` → <t:{unix}:t>", true)
                .AddField("Long Time", $"`<t:{unix}:T>` → <t:{unix}:T>", true)
                .AddField("Short Date", $"`<t:{unix}:d>` → <t:{unix}:d>", true)
                .AddField("Long Date", $"`<t:{unix}:D>` → <t:{unix}:D>", true)
                .AddField("Short DateTime", $"`<t:{unix}:f>` → <t:{unix}:f>", true)
                .AddField("Long DateTime", $"`<t:{unix}:F>` → <t:{unix}:F>", true)
                .AddField("Relative", $"`<t:{unix}:R>` → <t:{unix}:R>", true)
                .AddField("Unix", $"`{unix}`", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  SNOWFLAKE DECODER
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Snowflake(ulong id)
        {
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(id >> 22) + 1420070400000);
            var workerId = (id >> 17) & 0x1F;
            var processId = (id >> 12) & 0x1F;
            var increment = id & 0xFFF;

            var eb = CreateEmbed()
                .WithTitle("❄️ Snowflake Decoder")
                .AddField("ID", $"`{id}`", false)
                .AddField("Created", $"<t:{timestamp.ToUnixTimeSeconds()}:F> (<t:{timestamp.ToUnixTimeSeconds()}:R>)", false)
                .AddField("Worker ID", $"{workerId}", true)
                .AddField("Process ID", $"{processId}", true)
                .AddField("Increment", $"{increment}", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  CHARACTER/WORD COUNTER
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Count([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Provide some text to count!").SendAsync();
                return;
            }

            var chars = text.Length;
            var charsNoSpace = text.Replace(" ", "").Length;
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var lines = text.Split('\n').Length;
            var sentences = Regex.Matches(text, @"[.!?]+").Count;
            var vowels = Regex.Matches(text, @"[aeiouAEIOU]").Count;
            var digits = Regex.Matches(text, @"\d").Count;

            var eb = CreateEmbed()
                .WithTitle("📊 Text Analysis")
                .AddField("Characters", $"**{chars:N0}** (no spaces: {charsNoSpace:N0})", true)
                .AddField("Words", $"**{words:N0}**", true)
                .AddField("Lines", $"**{lines:N0}**", true)
                .AddField("Sentences", $"**{sentences:N0}**", true)
                .AddField("Vowels", $"**{vowels:N0}**", true)
                .AddField("Digits", $"**{digits:N0}**", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  COLOR INFO
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Color([Leftover] string hex = null)
        {
            int r, g, b;
            if (string.IsNullOrWhiteSpace(hex))
            {
                r = _rng.Next(256);
                g = _rng.Next(256);
                b = _rng.Next(256);
            }
            else
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, null, out var val))
                {
                    r = (val >> 16) & 0xFF;
                    g = (val >> 8) & 0xFF;
                    b = val & 0xFF;
                }
                else
                {
                    await Response().Error("Invalid hex! Use format: `#FF5733` or `FF5733`").SendAsync();
                    return;
                }
            }

            var hexStr = $"#{r:X2}{g:X2}{b:X2}";
            var h = 0.0;
            var s = 0.0;
            var l = 0.0;
            var rf = r / 255.0;
            var gf = g / 255.0;
            var bf = b / 255.0;
            var max = Math.Max(rf, Math.Max(gf, bf));
            var min = Math.Min(rf, Math.Min(gf, bf));
            l = (max + min) / 2;
            if (max != min)
            {
                var d = max - min;
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                if (max == rf) h = (gf - bf) / d + (gf < bf ? 6 : 0);
                else if (max == gf) h = (bf - rf) / d + 2;
                else h = (rf - gf) / d + 4;
                h /= 6;
            }

            var discordColor = new Discord.Color(r, g, b);
            var eb = CreateEmbed()
                .WithTitle($"🎨 Color: {hexStr}")
                .AddField("Hex", hexStr, true)
                .AddField("RGB", $"{r}, {g}, {b}", true)
                .AddField("HSL", $"{(int)(h * 360)}°, {(int)(s * 100)}%, {(int)(l * 100)}%", true)
                .AddField("Decimal", $"{(r << 16) | (g << 8) | b}", true)
                .WithColor(discordColor);

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  DICE ROLLER (RPG style)
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Roll([Leftover] string dice = "1d20")
        {
            var match = Regex.Match(dice.Trim(), @"^(\d+)?d(\d+)([+-]\d+)?$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                await Response().Error("Format: `NdS+M` (e.g. `2d6`, `1d20+5`, `4d8-2`)").SendAsync();
                return;
            }

            var count = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1;
            var sides = int.Parse(match.Groups[2].Value);
            var modifier = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

            count = Math.Clamp(count, 1, 100);
            sides = Math.Clamp(sides, 2, 1000);

            var rolls = new List<int>();
            for (var i = 0; i < count; i++)
                rolls.Add(_rng.Next(1, sides + 1));

            var total = rolls.Sum() + modifier;
            var rollStr = string.Join(", ", rolls);
            if (rollStr.Length > 200) rollStr = rollStr[..200] + "...";

            var modStr = modifier != 0 ? $" {(modifier > 0 ? "+" : "")}{modifier}" : "";
            var eb = CreateEmbed()
                .WithTitle($"🎲 {count}d{sides}{modStr}")
                .AddField("Rolls", $"[{rollStr}]", false)
                .AddField("Total", $"**{total}**", true)
                .WithOkColor();

            if (count == 1 && sides == 20)
            {
                if (rolls[0] == 20) eb.WithTitle("🎲 NAT 20! CRITICAL HIT!");
                else if (rolls[0] == 1) eb.WithTitle("🎲 NAT 1! CRITICAL FAIL!");
            }

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  COIN FLIP (enhanced)
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Flip(int times = 1)
        {
            times = Math.Clamp(times, 1, 100);
            var heads = 0;
            var tails = 0;
            var results = new List<string>();

            for (var i = 0; i < times; i++)
            {
                if (_rng.Next(2) == 0)
                {
                    heads++;
                    results.Add("🪙 H");
                }
                else
                {
                    tails++;
                    results.Add("⚫ T");
                }
            }

            if (times == 1)
            {
                await Response().Confirm($"🪙 **{(heads > 0 ? "Heads" : "Tails")}!**").SendAsync();
                return;
            }

            var resultsStr = string.Join(" ", results);
            if (resultsStr.Length > 300) resultsStr = resultsStr[..300] + "...";

            var eb = CreateEmbed()
                .WithTitle($"🪙 {times} Coin Flips")
                .AddField("Results", resultsStr, false)
                .AddField("Heads", $"**{heads}** ({heads * 100 / times}%)", true)
                .AddField("Tails", $"**{tails}** ({tails * 100 / times}%)", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  RANDOM NUMBER
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Random(int min = 1, int max = 100)
        {
            if (min > max) (min, max) = (max, min);
            var result = _rng.Next(min, max + 1);
            await Response().Confirm($"🎲 **{result}** (range: {min}-{max})").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  CHOOSE / PICK
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Choose([Leftover] string options)
        {
            if (string.IsNullOrWhiteSpace(options))
            {
                await Response().Error("Give me options separated by `|` or `,`!\nExample: `.tools choose pizza | tacos | sushi`").SendAsync();
                return;
            }

            var separator = options.Contains('|') ? '|' : ',';
            var choices = options.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (choices.Length < 2)
            {
                await Response().Error("Need at least 2 options!").SendAsync();
                return;
            }

            var choice = choices[_rng.Next(choices.Length)];
            await Response().Confirm($"🤔 I choose... **{choice}**!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  POLL (quick)
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuickPoll([Leftover] string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                await Response().Error("Ask a question! `.tools quickpoll Should we do game night?`").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("📊 Quick Poll")
                .WithDescription(question)
                .WithFooter($"Asked by {ctx.User.Username}")
                .WithOkColor();

            var msg = await Response().Embed(eb).SendAsync();
            var sentMsg = msg as Discord.Rest.RestUserMessage ?? (await ctx.Channel.GetMessageAsync(msg.Id)) as IUserMessage;
            if (sentMsg is not null)
            {
                await sentMsg.AddReactionAsync(new Emoji("👍"));
                await sentMsg.AddReactionAsync(new Emoji("👎"));
                await sentMsg.AddReactionAsync(new Emoji("🤷"));
            }
        }

        // ═══════════════════════════════════════════
        //  COUNTDOWN
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Countdown([Leftover] string dateStr)
        {
            if (!DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var target))
            {
                await Response().Error("Couldn't parse that date! Try: `2026-12-25` or `2026-12-31 23:59`").SendAsync();
                return;
            }

            var diff = target - DateTimeOffset.UtcNow;
            if (diff.TotalSeconds <= 0)
            {
                await Response().Confirm("⏰ That date has already passed!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("⏳ Countdown")
                .AddField("Target", $"<t:{target.ToUnixTimeSeconds()}:F>", false)
                .AddField("Time Remaining",
                    $"**{(int)diff.TotalDays}** days, **{diff.Hours}** hours, **{diff.Minutes}** minutes",
                    false)
                .AddField("Relative", $"<t:{target.ToUnixTimeSeconds()}:R>", false)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  BASE64 ENCODE/DECODE
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Base64Encode([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Provide text to encode!").SendAsync();
                return;
            }

            var encoded = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            await Response().Confirm($"🔐 **Encoded:**\n```{encoded}```").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Base64Decode([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Provide base64 to decode!").SendAsync();
                return;
            }

            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(text.Trim()));
                await Response().Confirm($"🔓 **Decoded:**\n```{decoded}```").SendAsync();
            }
            catch
            {
                await Response().Error("Invalid base64!").SendAsync();
            }
        }

        // ═══════════════════════════════════════════
        //  REVERSE TEXT
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Reverse([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Provide text to reverse!").SendAsync();
                return;
            }

            var reversed = new string(text.Reverse().ToArray());
            await Response().Confirm($"🔄 {reversed}").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  MOCK TEXT (SpOnGeBoB cAsE)
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Mock([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Provide text to mock!").SendAsync();
                return;
            }

            var result = new System.Text.StringBuilder();
            for (var i = 0; i < text.Length; i++)
                result.Append(i % 2 == 0 ? char.ToLower(text[i]) : char.ToUpper(text[i]));

            await Response().Confirm($"🧽 {result}").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  ASCII ART TEXT
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BigText([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await Response().Error("Provide text!").SendAsync();
                return;
            }

            text = text.ToUpper();
            if (text.Length > 10)
            {
                await Response().Error("Max 10 characters!").SendAsync();
                return;
            }

            var result = new System.Text.StringBuilder();
            foreach (var c in text)
            {
                if (c >= 'A' && c <= 'Z')
                    result.Append($":regional_indicator_{char.ToLower(c)}: ");
                else if (c >= '0' && c <= '9')
                    result.Append(c switch
                    {
                        '0' => ":zero: ", '1' => ":one: ", '2' => ":two: ", '3' => ":three: ",
                        '4' => ":four: ", '5' => ":five: ", '6' => ":six: ", '7' => ":seven: ",
                        '8' => ":eight: ", '9' => ":nine: ", _ => " "
                    });
                else if (c == ' ')
                    result.Append("   ");
                else if (c == '!')
                    result.Append(":exclamation: ");
                else if (c == '?')
                    result.Append(":question: ");
            }

            await Response().Confirm(result.ToString()).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  MEMBER COUNT
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Members()
        {
            var guild = (SocketGuild)ctx.Guild;
            var total = guild.MemberCount;
            var online = guild.Users.Count(u => u.Status != UserStatus.Offline && u.Status != UserStatus.Invisible);
            var bots = guild.Users.Count(u => u.IsBot);
            var humans = total - bots;

            var eb = CreateEmbed()
                .WithTitle($"👥 {guild.Name} — Members")
                .AddField("Total", $"**{total:N0}**", true)
                .AddField("Humans", $"**{humans:N0}**", true)
                .AddField("Bots", $"**{bots:N0}**", true)
                .AddField("Online", $"**{online:N0}**", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }
}
