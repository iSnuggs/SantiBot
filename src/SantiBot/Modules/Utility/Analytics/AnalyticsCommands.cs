#nullable disable
using SantiBot.Modules.Utility.Analytics;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Analytics")]
    [Group("analytics")]
    public partial class AnalyticsCommands : SantiModule<AnalyticsService>
    {
        private static readonly string[] DayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

        // ─── .analytics activity ─────────────────────────────────
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Activity()
        {
            var guild = (SocketGuild)ctx.Guild;
            var today = _service.GetMessagesPerDay(guild.Id, 1);
            var week = _service.GetMessagesPerDay(guild.Id, 7);
            var peakHours = _service.GetPeakHours(guild.Id);
            var engagement = _service.CalculateEngagement(guild);

            var todayCount = today.Sum(x => x.Count);
            var weekCount = week.Sum(x => x.Count);

            if (weekCount == 0)
            {
                await Response().Pending("No activity data recorded yet. Analytics begins tracking as messages are sent.").SendAsync();
                return;
            }

            var peakText = peakHours.Count > 0
                ? string.Join(", ", peakHours.Select(p => $"`{FormatHour(p.Hour)}` ({p.Count:N0})"))
                : "No data yet";

            // Build a mini daily chart for the last 7 days
            var maxDay = week.Max(x => x.Count);
            var dailyChart = string.Join('\n', week.Select(d =>
            {
                var bar = BuildBar(d.Count, maxDay, 15);
                return $"`{d.Date:ddd dd}` {bar} **{d.Count:N0}**";
            }));

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Server Activity — {guild.Name}")
                .AddField("Today", $"**{todayCount:N0}** messages", true)
                .AddField("This Week", $"**{weekCount:N0}** messages", true)
                .AddField("Engagement", $"**{engagement}%** of members active (7d)", true)
                .AddField("Peak Hours", peakText)
                .AddField("Daily Breakdown", dailyChart);

            await Response().Embed(eb).SendAsync();
        }

        // ─── .analytics topchannels ──────────────────────────────
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TopChannels(int days = 7)
        {
            if (days < 1 || days > 7)
            {
                await Response().Error("Days must be between 1 and 7.").SendAsync();
                return;
            }

            var channels = _service.GetMessagesPerChannel(ctx.Guild.Id, days);
            if (channels.Count == 0)
            {
                await Response().Error("No message data recorded yet. Analytics begins tracking when SantiBot is running.").SendAsync();
                return;
            }

            var top = channels.Take(15).ToList();
            var max = top[0].Count;

            var lines = top.Select((c, i) =>
            {
                var bar = BuildBar(c.Count, max, 12);
                return $"`{i + 1,2}.` <#{c.ChannelId}> {bar} **{c.Count:N0}**";
            });

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Top Channels (Last {days} days)")
                .WithDescription(string.Join('\n', lines));

            await Response().Embed(eb).SendAsync();
        }

        // ─── .analytics topposters ──────────────────────────────
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TopPosters(int days = 7)
        {
            if (days < 1 || days > 7)
            {
                await Response().Error("Days must be between 1 and 7.").SendAsync();
                return;
            }

            var posters = _service.GetTopPosters(ctx.Guild.Id, days, 15);
            if (posters.Count == 0)
            {
                await Response().Error("No message data recorded yet.").SendAsync();
                return;
            }

            var max = posters[0].Count;

            var lines = posters.Select((p, i) =>
            {
                var bar = BuildBar(p.Count, max, 12);
                var medal = i switch { 0 => " [1st]", 1 => " [2nd]", 2 => " [3rd]", _ => "" };
                return $"`{i + 1,2}.` <@{p.UserId}> {bar} **{p.Count:N0}**{medal}";
            });

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Top Posters (Last {days} days)")
                .WithDescription(string.Join('\n', lines));

            await Response().Embed(eb).SendAsync();
        }

        // ─── .analytics peakhours ────────────────────────────────
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PeakHours()
        {
            var hours = _service.GetMessagesPerHour(ctx.Guild.Id);
            var max = hours.Max();

            if (max == 0)
            {
                await Response().Error("No message data recorded yet.").SendAsync();
                return;
            }

            var peakList = _service.GetPeakHours(ctx.Guild.Id);
            var quietList = _service.GetQuietHours(ctx.Guild.Id);

            // Build 24-hour visual chart
            var lines = new List<string>(24);
            for (var h = 0; h < 24; h++)
            {
                var bar = BuildBar(hours[h], max, 20);
                lines.Add($"`{FormatHour(h)}` {bar} {hours[h]:N0}");
            }

            var peakText = string.Join(", ", peakList.Select(p => $"`{FormatHour(p.Hour)}`"));
            var quietText = string.Join(", ", quietList.Select(p => $"`{FormatHour(p.Hour)}`"));

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Server Activity by Hour (UTC)")
                .WithDescription(string.Join('\n', lines))
                .AddField("Peak Hours", peakText, true)
                .AddField("Quiet Hours", quietText, true);

            await Response().Embed(eb).SendAsync();
        }

        // ─── .analytics heatmap ──────────────────────────────────
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Heatmap()
        {
            var grid = _service.GetHeatmap(ctx.Guild.Id);
            var max = 0;
            for (var d = 0; d < 7; d++)
                for (var h = 0; h < 24; h++)
                    if (grid[d, h] > max)
                        max = grid[d, h];

            if (max == 0)
            {
                await Response().Error("No message data recorded yet.").SendAsync();
                return;
            }

            // Header row: hours (show every 3rd hour)
            var header = "       ";
            for (var h = 0; h < 24; h += 3)
                header += $"{h,2}  ";

            var lines = new List<string> { $"`{header}`" };

            for (var d = 0; d < 7; d++)
            {
                var row = $"{DayNames[d]}  ";
                for (var h = 0; h < 24; h++)
                {
                    var intensity = max > 0 ? (double)grid[d, h] / max : 0;
                    row += intensity switch
                    {
                        >= 0.75 => "██",
                        >= 0.50 => "▓▓",
                        >= 0.25 => "▒▒",
                        > 0     => "░░",
                        _       => "  "
                    };
                }

                lines.Add($"`{row}`");
            }

            // Legend
            lines.Add("");
            lines.Add("`██` High  `▓▓` Medium  `▒▒` Low  `░░` Minimal");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Activity Heatmap (Day x Hour, UTC)")
                .WithDescription(string.Join('\n', lines));

            await Response().Embed(eb).SendAsync();
        }

        // ─── .analytics growth ───────────────────────────────────
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Growth(int days = 30)
        {
            if (days < 1 || days > 30)
            {
                await Response().Error("Days must be between 1 and 30.").SendAsync();
                return;
            }

            var growth = _service.GetGrowth(ctx.Guild.Id, days);
            var totalJoins = growth.Sum(x => x.Joins);
            var totalLeaves = growth.Sum(x => x.Leaves);
            var totalNet = totalJoins - totalLeaves;

            if (totalJoins == 0 && totalLeaves == 0)
            {
                await Response().Error("No join/leave data recorded yet.").SendAsync();
                return;
            }

            var maxVal = growth.Max(x => Math.Max(x.Joins, x.Leaves));
            if (maxVal == 0) maxVal = 1;

            // Show last N days in a compact chart
            var displayDays = growth.TakeLast(Math.Min(days, 15)).ToList();
            var lines = displayDays.Select(d =>
            {
                var joinBar = BuildBar(d.Joins, maxVal, 8);
                var leaveBar = BuildBar(d.Leaves, maxVal, 8);
                var netSign = d.Net >= 0 ? "+" : "";
                return $"`{d.Date:MM/dd}` +{d.Joins} {joinBar} -{d.Leaves} {leaveBar} ({netSign}{d.Net})";
            });

            var netSign2 = totalNet >= 0 ? "+" : "";
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Member Growth (Last {days} days)")
                .WithDescription(string.Join('\n', lines))
                .AddField("Total Joins", $"**+{totalJoins:N0}**", true)
                .AddField("Total Leaves", $"**-{totalLeaves:N0}**", true)
                .AddField("Net Change", $"**{netSign2}{totalNet:N0}**", true);

            await Response().Embed(eb).SendAsync();
        }

        // ─── .analytics topwords ─────────────────────────────────
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TopWords(int count = 20)
        {
            if (count < 5 || count > 50)
            {
                await Response().Error("Count must be between 5 and 50.").SendAsync();
                return;
            }

            var words = _service.GetTopWords(ctx.Guild.Id, count);
            if (words.Count == 0)
            {
                await Response().Error("No word data recorded yet.").SendAsync();
                return;
            }

            var max = words[0].Count;

            var lines = words.Select((w, i) =>
            {
                var bar = BuildBar(w.Count, max, 10);
                return $"`{i + 1,2}.` **{w.Word}** {bar} {w.Count:N0}";
            });

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Most Used Words (Last 7 days)")
                .WithDescription(string.Join('\n', lines));

            await Response().Embed(eb).SendAsync();
        }

        // ─── .analytics serverreport (admin) ─────────────────────
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ServerReport()
        {
            var guild = (SocketGuild)ctx.Guild;
            var guildId = guild.Id;

            // Gather all metrics
            var todayMessages = _service.GetMessagesPerDay(guildId, 1).Sum(x => x.Count);
            var weekMessages = _service.GetMessagesPerDay(guildId, 7).Sum(x => x.Count);
            var engagement = _service.CalculateEngagement(guild);
            var peakHours = _service.GetPeakHours(guildId);
            var quietHours = _service.GetQuietHours(guildId);
            var topChannels = _service.GetMessagesPerChannel(guildId, 7).Take(5).ToList();
            var topPosters = _service.GetTopPosters(guildId, 7, 5);
            var growth = _service.GetGrowth(guildId, 7);
            var topWords = _service.GetTopWords(guildId, 10);

            var totalJoins = growth.Sum(x => x.Joins);
            var totalLeaves = growth.Sum(x => x.Leaves);
            var netGrowth = totalJoins - totalLeaves;
            var netSign = netGrowth >= 0 ? "+" : "";

            // Peak / quiet text
            var peakText = peakHours.Count > 0
                ? string.Join(", ", peakHours.Select(p => $"`{FormatHour(p.Hour)}`"))
                : "N/A";
            var quietText = quietHours.Count > 0
                ? string.Join(", ", quietHours.Select(p => $"`{FormatHour(p.Hour)}`"))
                : "N/A";

            // Top channels text
            var channelText = topChannels.Count > 0
                ? string.Join('\n', topChannels.Select((c, i) =>
                    $"`{i + 1}.` <#{c.ChannelId}> — **{c.Count:N0}** msgs"))
                : "No data";

            // Top posters text
            var posterText = topPosters.Count > 0
                ? string.Join('\n', topPosters.Select((p, i) =>
                    $"`{i + 1}.` <@{p.UserId}> — **{p.Count:N0}** msgs"))
                : "No data";

            // Top words text
            var wordText = topWords.Count > 0
                ? string.Join(", ", topWords.Select(w => $"**{w.Word}** ({w.Count:N0})"))
                : "No data";

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Server Report — {guild.Name}")
                .WithDescription($"Comprehensive analytics for the past 7 days.\nGenerated <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>")
                .AddField("Messages", $"Today: **{todayMessages:N0}** | Week: **{weekMessages:N0}**", false)
                .AddField("Engagement", $"**{engagement}%** of members active in the last 7 days", false)
                .AddField("Peak Hours (UTC)", peakText, true)
                .AddField("Quiet Hours (UTC)", quietText, true)
                .AddField("Member Growth (7d)", $"+{totalJoins:N0} joins / -{totalLeaves:N0} leaves (**{netSign}{netGrowth:N0}** net)", false)
                .AddField("Top Channels", channelText, true)
                .AddField("Top Posters", posterText, true)
                .AddField("Trending Words", wordText, false)
                .WithFooter($"Members: {guild.MemberCount:N0} | Channels: {guild.TextChannels.Count}");

            await Response().Embed(eb).SendAsync();
        }

        // ══════════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════════

        private static string BuildBar(int value, int max, int width)
        {
            if (max <= 0)
                return new string('░', width);

            var filled = (int)Math.Round((double)value / max * width);
            filled = Math.Clamp(filled, 0, width);

            return new string('█', filled) + new string('░', width - filled);
        }

        private static string FormatHour(int hour)
        {
            return hour switch
            {
                0 => "12 AM",
                12 => "12 PM",
                _ when hour < 12 => $"{hour} AM",
                _ => $"{hour - 12} PM"
            };
        }
    }
}
