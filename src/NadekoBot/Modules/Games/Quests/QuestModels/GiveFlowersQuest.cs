namespace NadekoBot.Modules.Games.Quests;

public sealed class GiveFlowersQuest : IQuest
{
    public QuestIds QuestId
        => QuestIds.GiveFlowers;

    public string Name
        => "Sharing is Caring";

    public string Desc
        => "Give 10 flowers to someone";

    public string ProgDesc
        => "flowers given";

    public QuestEventType EventType
        => QuestEventType.Give;

    public long RequiredAmount
        => 10;

    public long TryUpdateProgress(IDictionary<string, string> metadata, long oldProgress)
    {
        if (!metadata.TryGetValue("amount", out var amountStr)
            || !long.TryParse(amountStr, out var amount))
            return oldProgress;

        return oldProgress + amount;
    }
}