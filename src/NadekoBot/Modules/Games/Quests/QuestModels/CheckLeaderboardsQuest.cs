namespace NadekoBot.Modules.Games.Quests;

public sealed class CheckLeaderboardsQuest : IQuest
{
    public QuestIds QuestId
        => QuestIds.CheckBetting;

    public string Name
        => "Leaderboard Enthusiast";

    public string Desc
        => "Check lb, xplb and waifulb";

    public string ProgDesc
        => "";

    public QuestEventType EventType
        => QuestEventType.CommandUsed;

    public long RequiredAmount
        => 0b111;

    public long TryUpdateProgress(IDictionary<string, string> metadata, long oldProgress)
    {
        if (!metadata.TryGetValue("name", out var name))
            return oldProgress;

        var progress = oldProgress;

        if (name == "leaderboard")
            progress |= 0b001;
        else if (name == "xpleaderboard")
            progress |= 0b010;
        else if (name == "waifulb")
            progress |= 0b100;

        return progress;
    }

    public string ToString(long progress)
    {
        var msg = "";

        var emoji = IQuest.INCOMPLETE;
        if ((progress & 0b001) == 0b001)
            emoji = IQuest.COMPLETED;

        msg += emoji + " flower lb seen\n";

        emoji = IQuest.INCOMPLETE;
        if ((progress & 0b010) == 0b010)
            emoji = IQuest.COMPLETED;
            
        msg += emoji + " xp lb seen\n";
        
        emoji = IQuest.INCOMPLETE;
        if ((progress & 0b100) == 0b100)
            emoji = IQuest.COMPLETED;
            
        msg += emoji + " waifu lb seen";
        
        return msg;
    }
}