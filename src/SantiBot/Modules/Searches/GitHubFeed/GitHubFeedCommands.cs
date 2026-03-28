namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("GitHubFeed")]
    public partial class GitHubFeedCommands : SantiModule<GitHubFeedService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GhWatch(string repoFullName, ITextChannel channel)
        {
            if (!repoFullName.Contains('/'))
            {
                await Response().Error(strs.gh_invalid_repo).SendAsync();
                return;
            }

            var watch = await _service.WatchRepoAsync(ctx.Guild.Id, channel.Id, repoFullName);
            await Response().Confirm(strs.gh_watching(repoFullName, channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GhWatch(string repoFullName, string eventType, string toggle)
        {
            var enabled = toggle.ToLower() is "on" or "true" or "yes";
            var updated = await _service.ToggleEventTypeAsync(ctx.Guild.Id, repoFullName, eventType, enabled);

            if (updated)
                await Response().Confirm(strs.gh_event_toggled(eventType, enabled ? "ON" : "OFF", repoFullName)).SendAsync();
            else
                await Response().Error(strs.gh_not_watching(repoFullName)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GhUnwatch(string repoFullName)
        {
            var removed = await _service.UnwatchRepoAsync(ctx.Guild.Id, repoFullName);
            if (removed)
                await Response().Confirm(strs.gh_unwatched(repoFullName)).SendAsync();
            else
                await Response().Error(strs.gh_not_watching(repoFullName)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GhList()
        {
            var watches = await _service.ListWatchesAsync(ctx.Guild.Id);
            if (watches.Count == 0)
            {
                await Response().Error(strs.gh_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("GitHub Repo Watches");

            foreach (var w in watches)
            {
                var flags = new List<string>();
                if (w.WatchCommits) flags.Add("Commits");
                if (w.WatchIssues) flags.Add("Issues");
                if (w.WatchPRs) flags.Add("PRs");
                if (w.WatchReleases) flags.Add("Releases");

                eb.AddField(w.RepoFullName,
                    $"Channel: <#{w.ChannelId}> | Watching: {string.Join(", ", flags)}",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
