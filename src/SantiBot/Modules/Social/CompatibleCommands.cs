#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Compatibility")]
    [Group("compatible")]
    public partial class CompatibleCommands : SantiModule
    {
        private static readonly string[] _descriptions =
        {
            "Like oil and water... \U0001F62C",
            "This could be interesting... \U0001F914",
            "There's potential here! \U0001F60F",
            "A decent match! \U0001F60A",
            "Getting along nicely! \U0001F60D",
            "Great chemistry! \U0001F525",
            "Almost perfect match! \U0001F496",
            "Soulmates! \U0001F49E",
            "Literally the same person?! \U0001F92F",
            "COSMIC BOND! Fate brought you together! \U0001F31F"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Compatible(IUser user1, IUser user2 = null)
        {
            if (user2 is null)
            {
                user2 = user1;
                user1 = ctx.User;
            }

            // deterministic but fun percentage based on user IDs
            var combined = user1.Id ^ user2.Id;
            var seed = (int)(combined % int.MaxValue);
            var rng = new Random(seed);
            var percent = rng.Next(1, 101);

            var descIndex = Math.Min(percent / 11, _descriptions.Length - 1);
            var description = _descriptions[descIndex];

            var hearts = percent switch
            {
                >= 90 => "\U0001F49D\U0001F49D\U0001F49D\U0001F49D\U0001F49D",
                >= 70 => "\U0001F49D\U0001F49D\U0001F49D\U0001F49D",
                >= 50 => "\U0001F49D\U0001F49D\U0001F49D",
                >= 30 => "\U0001F49D\U0001F49D",
                _ => "\U0001F49D"
            };

            var eb = CreateEmbed()
                .WithTitle($"\U0001F48B Compatibility Test")
                .WithDescription(
                    $"{user1.Mention} x {user2.Mention}\n\n" +
                    $"{hearts}\n" +
                    $"**{percent}%** compatible!\n\n" +
                    $"{description}")
                .WithColor(percent >= 50 ? Discord.Color.Magenta : Discord.Color.LightGrey);

            await Response().Embed(eb).SendAsync();
        }
    }
}
