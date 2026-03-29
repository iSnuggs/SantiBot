#nullable disable
using SantiBot.Modules.Gambling.Jobs;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Jobs")]
    [Group("job")]
    public partial class JobCommands : SantiModule<JobService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task List()
        {
            var eb = CreateEmbed()
                .WithTitle("💼 Available Jobs")
                .WithOkColor();

            foreach (var (name, info) in JobService.JobTiers)
            {
                eb.AddField(name,
                    $"Pay: {info.Pay} 🥠\nCooldown: {info.CooldownHours}h\nRequires: {info.WorksToUnlock} shifts",
                    true);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Work()
        {
            var (success, message) = await _service.WorkAsync(ctx.Guild.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Apply([Leftover] string jobName)
        {
            var (success, message) = await _service.ApplyForJobAsync(ctx.Guild.Id, ctx.User.Id, jobName?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Career()
        {
            var job = await _service.GetUserJobAsync(ctx.Guild.Id, ctx.User.Id);
            if (job is null)
            {
                await Response().Error("You don't have a job yet! Use `.job apply Janitor` to start.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"📋 {ctx.User.Username}'s Career")
                .AddField("Current Job", job.JobName, true)
                .AddField("Total Shifts", job.TimesWorked.ToString(), true)
                .WithOkColor();

            // Show next unlock
            for (int i = 0; i < JobService.JobOrder.Length; i++)
            {
                if (JobService.JobOrder[i].Equals(job.JobName, StringComparison.OrdinalIgnoreCase) && i + 1 < JobService.JobOrder.Length)
                {
                    var next = JobService.JobOrder[i + 1];
                    var needed = JobService.JobTiers[next].WorksToUnlock;
                    eb.AddField("Next Promotion", $"{next} (need {needed} shifts, you have {job.TimesWorked})", false);
                    break;
                }
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Quit()
        {
            var (success, message) = await _service.QuitAsync(ctx.Guild.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
