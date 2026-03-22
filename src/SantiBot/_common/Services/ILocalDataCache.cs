#nullable disable
using SantiBot.Common.Pokemon;
using SantiBot.Modules.Games.Common.Trivia;

namespace SantiBot.Services;

public interface ILocalDataCache
{
    Task<IReadOnlyDictionary<string, SearchPokemon>> GetPokemonsAsync();
    Task<IReadOnlyDictionary<string, SearchPokemonAbility>> GetPokemonAbilitiesAsync();
    Task<TriviaQuestionModel[]> GetTriviaQuestionsAsync();
    Task<IReadOnlyDictionary<int, string>> GetPokemonMapAsync();
}