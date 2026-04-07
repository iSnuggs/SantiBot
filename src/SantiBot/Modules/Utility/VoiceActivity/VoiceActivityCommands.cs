namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("activity")]
    [Name("VoiceActivity")]
    public partial class VoiceActivityCommands : SantiModule<VoiceActivityService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Launch([Leftover] string activityName = "")
        {
            if (string.IsNullOrWhiteSpace(activityName))
            {
                // Show list by default
                await ActivityList();
                return;
            }

            var user = (IGuildUser)ctx.User;
            if (user.VoiceChannel is null)
            {
                await Response().Error(strs.activity_not_in_voice).SendAsync();
                return;
            }

            if (!VoiceActivityService.TryGetActivity(activityName, out var activity))
            {
                await Response().Error(strs.activity_not_found(activityName)).SendAsync();
                return;
            }

            var url = await _service.CreateActivityInviteAsync(user.VoiceChannel, activity.AppId);
            if (url is null)
            {
                await Response().Error(strs.activity_failed).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(activity.Name)
                .WithDescription(GetText(strs.activity_started(activity.Name, url)));

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ActivityList()
        {
            var activities = VoiceActivityService.GetActivities();

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.activity_list_title));

            foreach (var (key, (_, name)) in activities)
                eb.AddField(name, $"`.activity {key}`", true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
