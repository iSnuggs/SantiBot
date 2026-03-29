namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("AnimeTracker")]
    [Group("anime")]
    public partial class AnimeTrackerCommands : SantiModule<AnimeTracker.AnimeTrackerService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AnimeTrack([Leftover] string title)
        {
            var channel = (ITextChannel)ctx.Channel;
            var (success, aniId, resolvedTitle) = await _service.TrackAsync(ctx.Guild.Id, channel.Id, title);
            if (success)
                await Response()
                    .Confirm($"Now tracking **{resolvedTitle}** (AniList #{aniId}) in {channel.Mention}")
                    .SendAsync();
            else if (resolvedTitle is not null)
                await Response().Error($"Already tracking **{resolvedTitle}**.").SendAsync();
            else
                await Response().Error("Anime not found on AniList.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AnimeUntrack([Leftover] string title)
        {
            var success = await _service.UntrackAsync(ctx.Guild.Id, title);
            if (success)
                await Response().Confirm($"Stopped tracking **{title}**").SendAsync();
            else
                await Response().Error("Not tracking that anime.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AnimeList()
        {
            var tracks = await _service.ListAsync(ctx.Guild.Id);
            if (tracks.Count == 0)
            {
                await Response().Error("No anime being tracked.").SendAsync();
                return;
            }

            var desc = string.Join("\n", tracks.Select((t, i) =>
                $"`{i + 1}.` {t.Title} (Ep {t.LastEpisode}) -> <#{t.ChannelId}>"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Tracked Anime").WithDescription(desc))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AnimeSchedule()
        {
            await ctx.Channel.TriggerTypingAsync();
            var schedule = await _service.GetScheduleAsync();
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Upcoming Anime Episodes").WithDescription(schedule))
                .SendAsync();
        }
    }
}
