namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Name("ModAnalytics")]
    public partial class ModAnalyticsCommands : SantiModule<ModAnalyticsService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModStats()
            => await ShowGuildStats(30);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModStats(int days)
            => await ShowGuildStats(days);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModStats(IUser moderator)
        {
            var stats = await _service.GetModStatsAsync(ctx.Guild.Id, moderator.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Mod Stats for {moderator} (Last {stats.Days} Days)")
                .AddField("Total Actions", stats.TotalActions.ToString(), true)
                .AddField("Unique Targets", stats.UniqueTargets.ToString(), true)
                .AddField("\u200b", "\u200b", true)
                .AddField("Warns", stats.Warns.ToString(), true)
                .AddField("Mutes", stats.Mutes.ToString(), true)
                .AddField("Timeouts", stats.Timeouts.ToString(), true)
                .AddField("Kicks", stats.Kicks.ToString(), true)
                .AddField("Bans", stats.Bans.ToString(), true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModStatsTop()
            => await ModStatsTop(30);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModStatsTop(int days)
        {
            var topMods = await _service.GetTopModsAsync(ctx.Guild.Id, days);
            if (topMods.Count == 0)
            {
                await Response().Error(strs.modstats_none(days)).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Most Active Moderators (Last {days} Days)");

            var desc = "";
            for (var i = 0; i < topMods.Count; i++)
            {
                var (modId, count) = topMods[i];
                var medal = i switch
                {
                    0 => "#1",
                    1 => "#2",
                    2 => "#3",
                    _ => $"#{i + 1}"
                };
                desc += $"**{medal}** <@{modId}> - {count} actions\n";
            }

            eb.WithDescription(desc);
            await Response().Embed(eb).SendAsync();
        }

        private async Task ShowGuildStats(int days)
        {
            var stats = await _service.GetGuildStatsAsync(ctx.Guild.Id, days);
            if (stats.TotalActions == 0)
            {
                await Response().Error(strs.modstats_none(days)).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Moderation Summary (Last {stats.Days} Days)")
                .AddField("Total Actions", stats.TotalActions.ToString(), true)
                .AddField("Active Mods", stats.UniqueMods.ToString(), true)
                .AddField("Unique Targets", stats.UniqueTargets.ToString(), true)
                .AddField("Warns", stats.Warns.ToString(), true)
                .AddField("Mutes", stats.Mutes.ToString(), true)
                .AddField("Timeouts", stats.Timeouts.ToString(), true)
                .AddField("Kicks", stats.Kicks.ToString(), true)
                .AddField("Bans", stats.Bans.ToString(), true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
