#nullable disable
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("Animal Pictures")]
    [Group("animal")]
    public partial class AnimalPicsCommands : SantiModule
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders = { { "User-Agent", "SantiBot/1.0" } }
        };

        private static readonly SantiRandom _rng = new();

        private static readonly string[] _catFacts =
        [
            "Cats sleep 12-16 hours a day!",
            "A group of cats is called a 'clowder'.",
            "Cats have over 20 vocalizations including the purr.",
            "A cat's purr vibrates at 25-150 Hz, which can promote healing!",
            "Cats can rotate their ears 180 degrees.",
            "A cat's nose print is unique, like a human fingerprint.",
            "Cats spend 30-50% of their day grooming themselves.",
        ];

        private static readonly string[] _dogFacts =
        [
            "Dogs can understand up to 250 words and gestures!",
            "A dog's sense of smell is 10,000-100,000 times stronger than a human's.",
            "Dogs dream just like humans do!",
            "The Basenji is the only breed that doesn't bark.",
            "Dogs can detect diseases like cancer and diabetes by smell.",
            "A wagging tail doesn't always mean a happy dog.",
            "Dogs curl up when they sleep to protect their organs.",
        ];

        private static readonly string[] _birdFacts =
        [
            "Crows can recognize human faces!",
            "Hummingbirds can fly backwards.",
            "Parrots can live for over 80 years.",
            "Penguins propose to their mates with a pebble.",
            "Owls can rotate their heads 270 degrees.",
            "Flamingos are born white and turn pink from their diet.",
            "A woodpecker can peck 20 times per second.",
        ];

        private static readonly string[] _foxFacts =
        [
            "Foxes use the Earth's magnetic field to hunt!",
            "A fox's tail is called a 'brush'.",
            "Foxes can make over 40 different sounds.",
            "Arctic foxes can survive temperatures as low as -58°F.",
            "Foxes are the only canids that can retract their claws like cats.",
            "Baby foxes are called 'kits'.",
            "Foxes have whiskers on their legs to help them navigate.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Cat()
        {
            try
            {
                var json = await _http.GetStringAsync("https://api.thecatapi.com/v1/images/search");
                var match = Regex.Match(json, "\"url\":\"([^\"]+)\"");
                if (match.Success)
                {
                    var eb = CreateEmbed()
                        .WithTitle($"🐱 {_catFacts[_rng.Next(_catFacts.Length)]}")
                        .WithImageUrl(match.Groups[1].Value)
                        .WithOkColor();
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch a cat picture right now!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Dog()
        {
            try
            {
                var json = await _http.GetStringAsync("https://dog.ceo/api/breeds/image/random");
                var match = Regex.Match(json, "\"message\":\"([^\"]+)\"");
                if (match.Success)
                {
                    var eb = CreateEmbed()
                        .WithTitle($"🐶 {_dogFacts[_rng.Next(_dogFacts.Length)]}")
                        .WithImageUrl(match.Groups[1].Value.Replace("\\/", "/"))
                        .WithOkColor();
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch a dog picture right now!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bird()
        {
            try
            {
                var json = await _http.GetStringAsync("https://some-random-api.com/animal/bird");
                var imgMatch = Regex.Match(json, "\"image\":\"([^\"]+)\"");
                if (imgMatch.Success)
                {
                    var factMatch = Regex.Match(json, "\"fact\":\"([^\"]+)\"");
                    var fact = factMatch.Success ? factMatch.Groups[1].Value : _birdFacts[_rng.Next(_birdFacts.Length)];

                    var eb = CreateEmbed()
                        .WithTitle("🐦 Bird!")
                        .WithDescription(fact)
                        .WithImageUrl(imgMatch.Groups[1].Value)
                        .WithOkColor();
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch a bird picture right now!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Fox()
        {
            try
            {
                var json = await _http.GetStringAsync("https://randomfox.ca/floof/");
                var match = Regex.Match(json, "\"image\":\"([^\"]+)\"");
                if (match.Success)
                {
                    var eb = CreateEmbed()
                        .WithTitle($"🦊 {_foxFacts[_rng.Next(_foxFacts.Length)]}")
                        .WithImageUrl(match.Groups[1].Value.Replace("\\/", "/"))
                        .WithOkColor();
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch a fox picture right now!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bunny()
        {
            try
            {
                var json = await _http.GetStringAsync("https://api.bunnies.io/v2/loop/random/?media=gif");
                var match = Regex.Match(json, "\"gif\":\"([^\"]+)\"");
                if (match.Success)
                {
                    var eb = CreateEmbed()
                        .WithTitle("🐰 Bunny!")
                        .WithImageUrl(match.Groups[1].Value.Replace("\\/", "/"))
                        .WithOkColor();
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch a bunny picture right now!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Duck()
        {
            try
            {
                var json = await _http.GetStringAsync("https://random-d.uk/api/v2/random");
                var match = Regex.Match(json, "\"url\":\"([^\"]+)\"");
                if (match.Success)
                {
                    var eb = CreateEmbed()
                        .WithTitle("🦆 Quack!")
                        .WithImageUrl(match.Groups[1].Value.Replace("\\/", "/"))
                        .WithOkColor();
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch a duck picture right now!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Panda()
        {
            try
            {
                var json = await _http.GetStringAsync("https://some-random-api.com/animal/panda");
                var imgMatch = Regex.Match(json, "\"image\":\"([^\"]+)\"");
                if (imgMatch.Success)
                {
                    var factMatch = Regex.Match(json, "\"fact\":\"([^\"]+)\"");
                    var fact = factMatch.Success ? factMatch.Groups[1].Value : "Pandas eat 26-84 pounds of bamboo a day!";

                    var eb = CreateEmbed()
                        .WithTitle("🐼 Panda!")
                        .WithDescription(fact)
                        .WithImageUrl(imgMatch.Groups[1].Value)
                        .WithOkColor();
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch a panda picture right now!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Koala()
        {
            try
            {
                var json = await _http.GetStringAsync("https://some-random-api.com/animal/koala");
                var imgMatch = Regex.Match(json, "\"image\":\"([^\"]+)\"");
                if (imgMatch.Success)
                {
                    var factMatch = Regex.Match(json, "\"fact\":\"([^\"]+)\"");
                    var fact = factMatch.Success ? factMatch.Groups[1].Value : "Koalas sleep up to 22 hours a day!";

                    var eb = CreateEmbed()
                        .WithTitle("🐨 Koala!")
                        .WithDescription(fact)
                        .WithImageUrl(imgMatch.Groups[1].Value)
                        .WithOkColor();
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch a koala picture right now!").SendAsync();
        }
    }
}
