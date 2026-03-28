namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("schedrole")]
    [Name("ScheduledRoles")]
    public partial class ScheduledRoleCommands : SantiModule<ScheduledRoleService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task SchedRoleAdd(IGuildUser user, IRole role, [Leftover] string duration)
        {
            // Parse "in 7d" or just "7d"
            var durationStr = duration.Replace("in ", "", StringComparison.OrdinalIgnoreCase).Trim();
            var timeSpan = ScheduledRoleService.ParseDuration(durationStr);

            if (timeSpan is null || timeSpan.Value.TotalMinutes < 1)
            {
                await Response().Error(strs.schedrole_invalid_duration).SendAsync();
                return;
            }

            var scheduledFor = DateTime.UtcNow + timeSpan.Value;
            var grant = await _service.ScheduleAsync(
                ctx.Guild.Id, user.Id, role.Id, true, scheduledFor, ctx.User.Id);

            await Response().Confirm(strs.schedrole_added(
                role.Mention, user.Mention, scheduledFor.ToString("g"))).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task SchedRoleRemove(IGuildUser user, IRole role, [Leftover] string duration)
        {
            var durationStr = duration.Replace("in ", "", StringComparison.OrdinalIgnoreCase).Trim();
            var timeSpan = ScheduledRoleService.ParseDuration(durationStr);

            if (timeSpan is null || timeSpan.Value.TotalMinutes < 1)
            {
                await Response().Error(strs.schedrole_invalid_duration).SendAsync();
                return;
            }

            var scheduledFor = DateTime.UtcNow + timeSpan.Value;
            await _service.ScheduleAsync(
                ctx.Guild.Id, user.Id, role.Id, false, scheduledFor, ctx.User.Id);

            await Response().Confirm(strs.schedrole_removal_added(
                role.Mention, user.Mention, scheduledFor.ToString("g"))).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task SchedRoleList()
        {
            var pending = await _service.GetPendingAsync(ctx.Guild.Id);

            if (pending.Count == 0)
            {
                await Response().Error(strs.schedrole_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.schedrole_list_title));

            foreach (var g in pending)
            {
                var action = g.IsGrant ? "Grant" : "Remove";
                eb.AddField($"ID {g.Id} — {action} <@&{g.RoleId}>",
                    $"User: <@{g.UserId}>\nScheduled: {g.ScheduledFor:g} UTC\nBy: <@{g.ScheduledByUserId}>",
                    true);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task SchedRoleCancel(int id)
        {
            if (await _service.CancelAsync(ctx.Guild.Id, id))
                await Response().Confirm(strs.schedrole_cancelled(id)).SendAsync();
            else
                await Response().Error(strs.schedrole_not_found).SendAsync();
        }
    }
}
