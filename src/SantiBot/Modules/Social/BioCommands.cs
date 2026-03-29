#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Bio")]
    [Group("bio")]
    public partial class BioCommands : SantiModule<ProfileService>
    {
        private readonly DbService _db;

        public BioCommands(DbService db)
        {
            _db = db;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BioSet([Leftover] string bio)
        {
            if (string.IsNullOrWhiteSpace(bio))
            {
                await Response().Error("Please provide a bio!").SendAsync();
                return;
            }
            if (bio.Length > 200)
            {
                await Response().Error("Bio must be 200 characters or less!").SendAsync();
                return;
            }

            await _service.SetBioAsync(ctx.Guild.Id, ctx.User.Id, bio);
            await Response().Confirm("Bio updated!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Pronouns([Leftover] string pronouns)
        {
            if (string.IsNullOrWhiteSpace(pronouns))
            {
                await Response().Error("Please provide pronouns! (e.g., they/them, she/her, he/him)").SendAsync();
                return;
            }

            await _service.SetPronounsAsync(ctx.Guild.Id, ctx.User.Id, pronouns);
            await Response().Confirm($"Pronouns set to **{pronouns}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TimezoneSet([Leftover] string tz)
        {
            if (string.IsNullOrWhiteSpace(tz))
            {
                await Response().Error("Please provide a timezone! (e.g., EST, PST, UTC+5)").SendAsync();
                return;
            }

            await _service.SetTimezoneAsync(ctx.Guild.Id, ctx.User.Id, tz);
            await Response().Confirm($"Timezone set to **{tz}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SocialSet([Leftover] string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                await Response().Error("Usage: `.social set <platform> <handle>`").SendAsync();
                return;
            }

            var parts = input.Split(' ', 2);
            if (parts.Length < 2)
            {
                await Response().Error("Please specify platform and handle!").SendAsync();
                return;
            }

            var platform = parts[0].ToLowerInvariant();
            var handle = parts[1];

            await using var dbCtx = _db.GetDbContext();
            var existing = await dbCtx.GetTable<UserSocial>()
                .Where(x => x.UserId == ctx.User.Id && x.Platform == platform)
                .FirstOrDefaultAsyncLinqToDB();

            if (existing is not null)
            {
                await dbCtx.GetTable<UserSocial>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.Handle, handle)
                    .UpdateAsync();
            }
            else
            {
                await dbCtx.GetTable<UserSocial>()
                    .InsertAsync(() => new UserSocial
                    {
                        UserId = ctx.User.Id,
                        Platform = platform,
                        Handle = handle
                    });
            }

            await Response().Confirm($"**{platform}** handle set to **{handle}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Whois(IUser user = null)
        {
            user ??= ctx.User;
            var profile = await _service.GetOrCreateProfileAsync(ctx.Guild.Id, user.Id);

            await using var dbCtx = _db.GetDbContext();
            var socials = await dbCtx.GetTable<UserSocial>()
                .Where(x => x.UserId == user.Id)
                .ToListAsyncLinqToDB();

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle("Who Is")
                .AddField("Bio", string.IsNullOrEmpty(profile.Bio) ? "Not set" : profile.Bio)
                .AddField("Pronouns", string.IsNullOrEmpty(profile.Pronouns) ? "Not set" : profile.Pronouns, true)
                .AddField("Timezone", string.IsNullOrEmpty(profile.Timezone) ? "Not set" : profile.Timezone, true);

            if (socials.Count > 0)
            {
                var socialStr = string.Join("\n", socials.Select(s => $"**{s.Platform}**: {s.Handle}"));
                eb.AddField("Socials", socialStr);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
