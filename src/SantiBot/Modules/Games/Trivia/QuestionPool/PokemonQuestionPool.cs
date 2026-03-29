namespace SantiBot.Modules.Games.Common.Trivia;

public sealed class PokemonQuestionPool : IQuestionPool
{
    public int QuestionsCount => 905; // xd
    private readonly SantiRandom _rng;
    private readonly ILocalDataCache _cache;

    public PokemonQuestionPool(ILocalDataCache cache)
    {
        _cache = cache;
        _rng = new SantiRandom();
    }

    public async Task<TriviaQuestion?> GetQuestionAsync()
    {
        var pokes = await _cache.GetPokemonMapAsync();

        if (pokes is null or { Count: 0 })
            return default;
            
        var num = _rng.Next(1, QuestionsCount + 1);
        return new(new()
        {
            Question = "Who's That Pokémon?",
            Answer = pokes[num].ToTitleCase(),
            Category = "Pokemon",
            ImageUrl = $@"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{num}.png",
            AnswerImageUrl = $@"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{num}.png"
        });
    }
}