using SantiBot.Modules.Gambling.Common;
using SantiBot.Modules.Gambling.Pets;
using SantiBot.Modules.Gambling.Services;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Pets (Legacy)")]
    [Group("oldpet")]
    public partial class PetCommands : GamblingModule<PetService>
    {
        private readonly PetService _pets;
        private readonly DiscordSocketClient _client;

        public PetCommands(GamblingConfigService gcs,
            PetService pets,
            DiscordSocketClient client) : base(gcs)
        {
            _pets = pets;
            _client = client;
        }

        [Cmd]
        public async Task PetAdopt(string species, [Leftover] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 32)
            {
                await Response().Error(strs.pet_name_invalid).SendAsync();
                return;
            }

            // Capitalize species for matching
            species = char.ToUpper(species[0]) + species[1..].ToLower();

            var (success, error) = await _pets.AdoptAsync(ctx.User.Id, species, name);

            if (!success)
            {
                var msg = error switch
                {
                    "invalid_species" => strs.pet_invalid_species(string.Join(", ", PetService.ValidSpecies)),
                    "already_have_pet" => strs.pet_already_have,
                    _ => strs.pet_adopt_fail
                };
                await Response().Error(msg).SendAsync();
                return;
            }

            await Response().Confirm(strs.pet_adopted(name, species)).SendAsync();
        }

        [Cmd]
        public async Task Pet()
        {
            var (pet, hunger, happiness) = await _pets.GetPetAsync(ctx.User.Id);

            if (pet is null)
            {
                await Response().Error(strs.pet_none).SendAsync();
                return;
            }

            var xpNeeded = PetService.XpForLevel(pet.Level);
            var hungerBar = BuildBar(hunger, 100);
            var happinessBar = BuildBar(happiness, 100);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{pet.Name} the {pet.Species}")
                .AddField(GetText(strs.pet_level), $"Lv. {pet.Level} ({pet.Xp}/{xpNeeded} XP)", true)
                .AddField(GetText(strs.pet_hunger), $"{hungerBar} {hunger}/100", false)
                .AddField(GetText(strs.pet_happiness), $"{happinessBar} {happiness}/100", false);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        public async Task PetFeed()
        {
            var (success, error) = await _pets.FeedPetAsync(ctx.User.Id);

            if (!success)
            {
                var msg = error switch
                {
                    "no_pet" => strs.pet_none,
                    "not_enough" => strs.not_enough(CurrencySign),
                    _ => strs.pet_feed_fail
                };
                await Response().Error(msg).SendAsync();
                return;
            }

            await Response().Confirm(strs.pet_fed).SendAsync();
        }

        [Cmd]
        public async Task PetPlay()
        {
            var (success, error, cooldown) = await _pets.PlayWithPetAsync(ctx.User.Id);

            if (!success)
            {
                if (error == "cooldown" && cooldown.HasValue)
                {
                    await Response().Error(strs.pet_play_cooldown((int)cooldown.Value.TotalMinutes)).SendAsync();
                    return;
                }
                var msg = error switch
                {
                    "no_pet" => strs.pet_none,
                    _ => strs.pet_play_fail
                };
                await Response().Error(msg).SendAsync();
                return;
            }

            await Response().Confirm(strs.pet_played).SendAsync();
        }

        [Cmd]
        public async Task PetRename([Leftover] string newName)
        {
            if (string.IsNullOrWhiteSpace(newName) || newName.Length > 32)
            {
                await Response().Error(strs.pet_name_invalid).SendAsync();
                return;
            }

            var (success, error) = await _pets.RenamePetAsync(ctx.User.Id, newName);

            if (!success)
            {
                await Response().Error(strs.pet_none).SendAsync();
                return;
            }

            await Response().Confirm(strs.pet_renamed(newName)).SendAsync();
        }

        [Cmd]
        public async Task PetLeaderboard()
        {
            var top = await _pets.GetLeaderboardAsync();

            if (top.Count == 0)
            {
                await Response().Error(strs.pet_lb_empty).SendAsync();
                return;
            }

            var desc = string.Join('\n', top.Select((p, i) =>
            {
                var user = _client.GetUser(p.UserId);
                return $"`#{i + 1}` **{p.Name}** the {p.Species} - Lv. {p.Level} - {user?.ToString() ?? p.UserId.ToString()}";
            }));

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.pet_lb_title))
                .WithDescription(desc);

            await Response().Embed(eb).SendAsync();
        }

        private static string BuildBar(int value, int max)
        {
            const int barLength = 10;
            var filled = (int)Math.Round(value / (double)max * barLength);
            return new string('\u2588', filled) + new string('\u2591', barLength - filled);
        }
    }
}
