#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Heatmap")]
    [Group("heatmap")]
    public partial class HeatmapCommands : SantiModule<HeatmapService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Heatmap(IUser user = null, int days = 30)
        {
            user ??= ctx.User;
            days = Math.Clamp(days, 7, 365);

            var data = await _service.GetHeatmapAsync(ctx.Guild.Id, user.Id, days);
            var rendered = HeatmapService.RenderHeatmap(data, days);

            var totalMessages = data.Sum(x => x.MessageCount);
            var activeDays = data.Count;
            var avgPerDay = activeDays > 0 ? totalMessages / activeDays : 0;

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle("\U0001F4CA Activity Heatmap")
                .WithDescription(rendered)
                .AddField("Total Messages", totalMessages.ToString("N0"), true)
                .AddField("Active Days", $"{activeDays}/{days}", true)
                .AddField("Avg/Day", avgPerDay.ToString("N0"), true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
