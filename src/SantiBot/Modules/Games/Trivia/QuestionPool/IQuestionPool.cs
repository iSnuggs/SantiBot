namespace SantiBot.Modules.Games.Common.Trivia;

public interface IQuestionPool
{
    Task<TriviaQuestion?> GetQuestionAsync();
}