#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("modshift")]
    public partial class ModShiftCommands : SantiModule<ModShiftService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModShiftAdd(IGuildUser user, string dayStr, int startHour, int endHour)
        {
            if (!Enum.TryParse<DayOfWeek>(dayStr, true, out var day))
            {
                await Response().Error("Invalid day. Use: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday").SendAsync();
                return;
            }

            if (startHour < 0 || startHour > 23 || endHour < 0 || endHour > 23)
            {
                await Response().Error("Hours must be between 0 and 23.").SendAsync();
                return;
            }

            var shift = await _service.AddShiftAsync(ctx.Guild.Id, user.Id, day, startHour, endHour);
            await Response().Confirm($"Mod shift **#{shift.Id}** created.\n{user.Mention} | {day} {startHour}:00-{endHour}:00 UTC").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModShiftList()
        {
            var shifts = await _service.ListShiftsAsync(ctx.Guild.Id);
            if (shifts.Count == 0)
            {
                await Response().Error("No mod shifts configured.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Mod Shift Schedule")
                .WithOkColor();

            foreach (var g in shifts.GroupBy(x => x.DayOfWeek))
            {
                var lines = g.Select(s => $"#{s.Id} <@{s.UserId}> {s.StartHour}:00-{s.EndHour}:00 UTC");
                embed.AddField(g.Key.ToString(), string.Join("\n", lines));
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModShiftDel(int id)
        {
            if (await _service.RemoveShiftAsync(ctx.Guild.Id, id))
                await Response().Confirm($"Mod shift **#{id}** deleted.").SendAsync();
            else
                await Response().Error("Shift not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ModShiftOnDuty()
        {
            var onDuty = await _service.GetOnDutyAsync(ctx.Guild.Id);
            if (onDuty.Count == 0)
            {
                await Response().Error("No moderators are currently on duty.").SendAsync();
                return;
            }

            var mentions = string.Join(", ", onDuty.Select(s => $"<@{s.UserId}>"));
            await Response().Confirm($"Currently on duty: {mentions}").SendAsync();
        }
    }
}
