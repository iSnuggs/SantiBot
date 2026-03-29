namespace SantiBot.Modules.Games.Quests;

public sealed class BetFlowersQuest : IQuest
{
    public QuestIds QuestId
        => QuestIds.BetFlowers;

    public string Name
        => "Fortune Gambler";

    public string Desc
        => "Bet 300 fortune cookies";

    public string ProgDesc
        => "fortune cookies bet";

    public QuestEventType EventType
        => QuestEventType.BetPlaced;

    public long RequiredAmount
        => 300;

    public long TryUpdateProgress(IDictionary<string, string> metadata, long oldProgress)
    {
        if (!metadata.TryGetValue("amount", out var amountStr)
            || !long.TryParse(amountStr, out var amount))
            return oldProgress;

        return oldProgress + amount;
    }
}