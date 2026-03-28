namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Birthday")]
    [Group("birthday")]
    public partial class BirthdayCommands : SantiModule<BirthdayService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(2)]
        public async Task Birthday(int month, int day)
        {
            if (month < 1 || month > 12 || day < 1 || day > 31)
            {
                await Response().Error(strs.birthday_invalid_date).SendAsync();
                return;
            }

            // Validate the day is valid for the month
            try
            {
                _ = new DateTime(2000, month, day);
            }
            catch
            {
                await Response().Error(strs.birthday_invalid_date).SendAsync();
                return;
            }

            await _service.SetBirthdayAsync(ctx.Guild.Id, ctx.User.Id, month, day);
            await Response().Confirm(strs.birthday_set(month, day)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Birthday(IGuildUser? user = null)
        {
            var targetUser = user ?? (IGuildUser)ctx.User;
            var birthday = await _service.GetBirthdayAsync(ctx.Guild.Id, targetUser.Id);

            if (birthday is null)
            {
                await Response().Error(strs.birthday_not_set(targetUser.Mention)).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.birthday_title))
                .WithDescription(GetText(strs.birthday_info(targetUser.Mention, birthday.Month, birthday.Day)));

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task BirthdayUpcoming()
        {
            var upcoming = await _service.GetUpcomingAsync(ctx.Guild.Id, 30);

            if (!upcoming.Any())
            {
                await Response().Error(strs.birthday_none_upcoming).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.birthday_upcoming_title));

            foreach (var b in upcoming)
            {
                var user = await ctx.Guild.GetUserAsync(b.UserId);
                var name = user?.DisplayName ?? b.UserId.ToString();
                eb.AddField(name, $"{b.Month}/{b.Day}", true);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BirthdayChannel(ITextChannel channel)
        {
            await _service.SetChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.birthday_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BirthdayRole(IRole role)
        {
            await _service.SetRoleAsync(ctx.Guild.Id, role.Id);
            await Response().Confirm(strs.birthday_role_set(role.Mention)).SendAsync();
        }
    }
}
