#nullable disable
using SantiBot.Modules.Searches.Services;

namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class JokeCommands : SantiModule<SearchesService>
    {
        [Cmd]
        public async Task Yomama()
            => await Response().Confirm(await _service.GetYomamaJoke()).SendAsync();

        [Cmd]
        public async Task Randjoke()
        {
            var (setup, punchline) = await _service.GetRandomJoke();
            await Response().Confirm(setup, punchline).SendAsync();
        }

        [Cmd]
        public async Task ChuckNorris()
            => await Response().Confirm(await _service.GetChuckNorrisJoke()).SendAsync();

        [Cmd]
        public async Task WowJoke()
        {
            if (!_service.WowJokes.Any())
            {
                await Response().Error(strs.jokes_not_loaded).SendAsync();
                return;
            }

            var joke = _service.WowJokes[new SantiRandom().Next(0, _service.WowJokes.Count)];
            await Response().Confirm(joke.Question, joke.Answer).SendAsync();
        }

        [Cmd]
        public async Task MagicItem()
        {
            if (!_service.MagicItems.Any())
            {
                await Response().Error(strs.magicitems_not_loaded).SendAsync();
                return;
            }

            var item = _service.MagicItems[new SantiRandom().Next(0, _service.MagicItems.Count)];

            await Response().Confirm("✨" + item.Name, item.Description).SendAsync();
        }
    }
}