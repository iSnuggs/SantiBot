#nullable disable
using SantiBot.Db.Models;
using SantiBot.Modules.Games.Voice;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Voice Tools")]
    [Group("vt")]
    public partial class VoiceToolsCommands : SantiModule<VoiceToolsService>
    {
        // ═══════════════════════════════════════════
        //  SOUNDBOARD
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task SoundAdd(string name, [Leftover] string url)
        {
            await _service.AddSoundAsync(ctx.Guild.Id, name, url, ctx.User.Id);
            await Response().Confirm($"🔊 Sound **{name}** added to the soundboard!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Sounds()
        {
            var sounds = await _service.GetSoundsAsync(ctx.Guild.Id);
            if (sounds.Count == 0)
            {
                await Response().Confirm("No sounds on the soundboard! Admins: `.vt soundadd <name> <url>`").SendAsync();
                return;
            }

            var grouped = sounds.GroupBy(s => s.Category);
            var sb = new System.Text.StringBuilder();
            foreach (var group in grouped)
            {
                sb.AppendLine($"**{group.Key}:**");
                foreach (var s in group)
                    sb.AppendLine($"  🔊 `{s.Name}` — played {s.PlayCount}x");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithTitle($"🔊 Soundboard ({sounds.Count} sounds)")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task SoundRemove([Leftover] string name)
        {
            var success = await _service.RemoveSoundAsync(ctx.Guild.Id, name);
            if (success)
                await Response().Confirm($"Sound **{name}** removed!").SendAsync();
            else
                await Response().Error("Sound not found!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  TEMP VOICE CHANNELS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task TempVoiceSetup(IVoiceChannel createChannel)
        {
            var category = (createChannel as SocketVoiceChannel)?.Category;
            await _service.SetTempVoiceConfigAsync(ctx.Guild.Id, createChannel.Id, category?.Id ?? 0, "{user}'s Channel");
            await Response().Confirm($"Temp voice channels enabled! When users join **{createChannel.Name}**, a personal channel will be created.").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  VOICE STATS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task VoiceLeaderboard(int days = 7)
        {
            days = Math.Clamp(days, 1, 30);
            var leaders = await _service.GetVoiceLeaderboardAsync(ctx.Guild.Id, days);

            if (leaders.Count == 0)
            {
                await Response().Confirm("No voice activity tracked yet!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            var rank = 1;
            foreach (var (userId, totalMinutes) in leaders)
            {
                var medal = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{rank}" };
                var hours = totalMinutes / 60;
                var mins = totalMinutes % 60;
                sb.AppendLine($"{medal} <@{userId}> — **{hours}h {mins}m**");
                rank++;
            }

            var eb = CreateEmbed()
                .WithTitle($"🎤 Voice Leaderboard (Last {days} days)")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MyVoice(IUser user = null)
        {
            user ??= ctx.User;
            var (totalMin, sessions, streamMin) = await _service.GetUserVoiceStatsAsync(user.Id, ctx.Guild.Id);
            var hours = totalMin / 60;
            var mins = totalMin % 60;
            var streamH = streamMin / 60;
            var streamM = streamMin % 60;

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle("🎤 Voice Stats")
                .AddField("Total Time", $"**{hours}h {mins}m**", true)
                .AddField("Sessions", $"**{sessions}**", true)
                .AddField("Stream Time", $"**{streamH}h {streamM}m**", true)
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  CHANNEL POINTS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Points(IUser user = null)
        {
            user ??= ctx.User;
            var config = await _service.GetPointsConfigAsync(ctx.Guild.Id);
            var points = await _service.GetUserPointsAsync(user.Id, ctx.Guild.Id);

            await Response().Confirm($"{config.PointsEmoji} **{user.Username}** has **{points:N0}** {config.PointsName}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PointRewards()
        {
            var config = await _service.GetPointsConfigAsync(ctx.Guild.Id);
            var rewards = await _service.GetRewardsAsync(ctx.Guild.Id);

            if (rewards.Count == 0)
            {
                await Response().Confirm($"No {config.PointsName} rewards set up yet!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var r in rewards)
                sb.AppendLine($"{config.PointsEmoji} **{r.Name}** — {r.Cost:N0} points\n  *{r.Description}*\n");

            var eb = CreateEmbed()
                .WithTitle($"{config.PointsEmoji} {config.PointsName} Rewards")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  PREDICTIONS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Predict(string option1, string option2, [Leftover] string question)
        {
            var pred = await _service.CreatePredictionAsync(ctx.Guild.Id, ctx.Channel.Id, ctx.User.Id, question, option1, option2);

            var eb = CreateEmbed()
                .WithTitle("🔮 PREDICTION STARTED!")
                .WithDescription($"**{question}**")
                .AddField($"1️⃣ {option1}", "0 points (0 voters)", true)
                .AddField($"2️⃣ {option2}", "0 points (0 voters)", true)
                .WithFooter("Use .vt bet <1 or 2> <points> to place your bet!")
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bet(int option, long points)
        {
            if (points < 1)
            {
                await Response().Error("Bet at least 1 point!").SendAsync();
                return;
            }

            var (success, message) = await _service.PlaceBetAsync(ctx.Guild.Id, ctx.User.Id, option, points);
            if (success)
                await Response().Confirm($"🔮 {message}").SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task PredictResolve(int winningOption)
        {
            var (success, message) = await _service.ResolvePredictionAsync(ctx.Guild.Id, winningOption);
            if (success)
                await Response().Confirm($"🔮 **Prediction Resolved!**\n\n{message}").SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PredictStatus()
        {
            var pred = await _service.GetActivePredictionAsync(ctx.Guild.Id);
            if (pred is null)
            {
                await Response().Confirm("No active prediction right now!").SendAsync();
                return;
            }

            var total = pred.Option1Points + pred.Option2Points;
            var pct1 = total > 0 ? pred.Option1Points * 100 / total : 50;
            var pct2 = total > 0 ? pred.Option2Points * 100 / total : 50;

            var bar1 = new string('█', (int)(pct1 / 5)) + new string('░', 20 - (int)(pct1 / 5));
            var bar2 = new string('█', (int)(pct2 / 5)) + new string('░', 20 - (int)(pct2 / 5));

            var eb = CreateEmbed()
                .WithTitle("🔮 Active Prediction")
                .WithDescription($"**{pred.Question}**")
                .AddField($"1️⃣ {pred.Option1}", $"[{bar1}] {pred.Option1Points:N0} pts ({pred.Option1Voters} voters)", false)
                .AddField($"2️⃣ {pred.Option2}", $"[{bar2}] {pred.Option2Points:N0} pts ({pred.Option2Voters} voters)", false)
                .AddField("Total Pool", $"**{total:N0}** points", true)
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════
        //  FAN ART
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FanArtSubmit(string title, [Leftover] string imageUrl)
        {
            var submission = await _service.SubmitFanArtAsync(ctx.Guild.Id, ctx.User.Id, title, imageUrl);
            await Response().Confirm($"🎨 Fan art **{title}** submitted! An admin will review it.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FanArtGallery()
        {
            var art = await _service.GetFanArtAsync(ctx.Guild.Id);
            if (art.Count == 0)
            {
                await Response().Confirm("No fan art yet! Submit with `.vt fanartsubmit <title> <imageUrl>`").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            var rank = 1;
            foreach (var a in art)
            {
                sb.AppendLine($"**{rank}.** 🎨 **{a.Title}** by <@{a.UserId}> — {a.Votes} votes");
                rank++;
            }

            var eb = CreateEmbed()
                .WithTitle($"🎨 Fan Art Gallery ({art.Count} pieces)")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FanArtVote(int submissionId)
        {
            await _service.VoteFanArtAsync(submissionId);
            await Response().Confirm("🎨 Vote recorded!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  FEED SUBSCRIPTIONS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task FeedAdd(string feedType, ITextChannel channel, [Leftover] string feedUrl)
        {
            var validTypes = new[] { "YouTube", "Twitch", "Reddit", "RSS", "Twitter", "Steam", "Weather" };
            if (!validTypes.Any(t => t.Equals(feedType, StringComparison.OrdinalIgnoreCase)))
            {
                await Response().Error($"Valid feed types: {string.Join(", ", validTypes.Select(t => $"`{t}`"))}").SendAsync();
                return;
            }

            var feed = await _service.AddFeedAsync(ctx.Guild.Id, channel.Id, feedType, feedUrl, feedUrl, ctx.User.Id);
            await Response().Confirm($"📡 **{feedType}** feed added to {channel.Mention}!\nURL: {feedUrl}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Feeds()
        {
            var feeds = await _service.GetFeedsAsync(ctx.Guild.Id);
            if (feeds.Count == 0)
            {
                await Response().Confirm("No feed subscriptions! Admins: `.vt feedadd <type> #channel <url>`").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            var grouped = feeds.GroupBy(f => f.FeedType);
            foreach (var group in grouped)
            {
                sb.AppendLine($"**{group.Key}:**");
                foreach (var f in group)
                    sb.AppendLine($"  📡 {f.FeedName} → <#{f.ChannelId}>");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithTitle($"📡 Feed Subscriptions ({feeds.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task FeedRemove(int feedId)
        {
            var success = await _service.RemoveFeedAsync(ctx.Guild.Id, feedId);
            if (success)
                await Response().Confirm("Feed removed!").SendAsync();
            else
                await Response().Error("Feed not found!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  UPTIME MONITORING
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task MonitorAdd(ITextChannel alertChannel, string url, [Leftover] string name)
        {
            var monitor = await _service.AddMonitorAsync(ctx.Guild.Id, alertChannel.Id, url, name);
            await Response().Confirm($"🟢 Monitoring **{name}** ({url})\nAlerts → {alertChannel.Mention}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Monitors()
        {
            var monitors = await _service.GetMonitorsAsync(ctx.Guild.Id);
            if (monitors.Count == 0)
            {
                await Response().Confirm("No uptime monitors! Admins: `.vt monitoradd #alerts <url> <name>`").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var m in monitors)
            {
                var status = m.IsUp ? "🟢 UP" : "🔴 DOWN";
                sb.AppendLine($"{status} **{m.Name}** — `{m.Url}`");
                if (!m.IsUp && m.LastDownAt.HasValue)
                    sb.AppendLine($"  Down since: <t:{new DateTimeOffset(m.LastDownAt.Value).ToUnixTimeSeconds()}:R>");
            }

            var eb = CreateEmbed()
                .WithTitle($"📡 Uptime Monitors ({monitors.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task MonitorRemove(int monitorId)
        {
            var success = await _service.RemoveMonitorAsync(ctx.Guild.Id, monitorId);
            if (success)
                await Response().Confirm("Monitor removed!").SendAsync();
            else
                await Response().Error("Monitor not found!").SendAsync();
        }
    }
}
