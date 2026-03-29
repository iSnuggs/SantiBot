#nullable disable
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Profile")]
    [Group("profile")]
    public partial class ProfileCommands : SantiModule<ProfileService>
    {
        private readonly BackgroundShopService _bgService;
        private readonly AchievementService _achievements;

        public ProfileCommands(BackgroundShopService bgService, AchievementService achievements)
        {
            _bgService = bgService;
            _achievements = achievements;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Profile(IUser user = null)
        {
            user ??= ctx.User;
            var profile = await _service.GetOrCreateProfileAsync(ctx.Guild.Id, user.Id);
            var achievements = await _achievements.GetAchievementsAsync(ctx.Guild.Id, user.Id);

            var badgeStr = achievements.Count > 0
                ? string.Join(" ", achievements.Take(10).Select(a => a.Emoji))
                : "None yet";

            var eb = CreateEmbed()
                .WithTitle($"{profile.BackgroundName} Theme")
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithColor(HexToColor(profile.BackgroundColor))
                .AddField("Title", string.IsNullOrEmpty(profile.Title) ? "No title set" : profile.Title, true)
                .AddField("Pronouns", string.IsNullOrEmpty(profile.Pronouns) ? "Not set" : profile.Pronouns, true)
                .AddField("Timezone", string.IsNullOrEmpty(profile.Timezone) ? "Not set" : profile.Timezone, true)
                .AddField("Messages", profile.MessageCount.ToString("N0"), true)
                .AddField("Joined", user is IGuildUser gu ? gu.JoinedAt?.ToString("MMM dd, yyyy") ?? "Unknown" : "Unknown", true)
                .AddField("Badges", badgeStr)
                .AddField("Bio", string.IsNullOrEmpty(profile.Bio) ? "No bio set. Use `.profile set bio <text>`" : profile.Bio);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ProfileSet([Leftover] string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                await Response().Error("Usage: `.profile set bio <text>` or `.profile set title <text>`").SendAsync();
                return;
            }

            var parts = input.Split(' ', 2);
            if (parts.Length < 2)
            {
                await Response().Error("Please specify what to set and the value.").SendAsync();
                return;
            }

            var field = parts[0].ToLowerInvariant();
            var value = parts[1];

            switch (field)
            {
                case "bio":
                    if (value.Length > 200)
                    {
                        await Response().Error("Bio must be 200 characters or less!").SendAsync();
                        return;
                    }
                    await _service.SetBioAsync(ctx.Guild.Id, ctx.User.Id, value);
                    await Response().Confirm($"Bio updated!").SendAsync();
                    break;
                case "title":
                    if (value.Length > 50)
                    {
                        await Response().Error("Title must be 50 characters or less!").SendAsync();
                        return;
                    }
                    await _service.SetTitleAsync(ctx.Guild.Id, ctx.User.Id, value);
                    await Response().Confirm($"Title updated!").SendAsync();
                    break;
                default:
                    await Response().Error("Unknown field. Use `bio` or `title`.").SendAsync();
                    break;
            }
        }

        private static Discord.Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length < 7) return new Discord.Color(88, 101, 242);
            try
            {
                var r = Convert.ToByte(hex.Substring(1, 2), 16);
                var g = Convert.ToByte(hex.Substring(3, 2), 16);
                var b = Convert.ToByte(hex.Substring(5, 2), 16);
                return new Discord.Color(r, g, b);
            }
            catch { return new Discord.Color(88, 101, 242); }
        }
    }
}
