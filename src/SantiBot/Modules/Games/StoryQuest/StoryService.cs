#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Games.StoryQuest;

public sealed class StoryService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    public StoryService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public class StoryChapter
    {
        public string Text { get; set; }
        public Dictionary<string, int> Choices { get; set; } // choice -> next chapter index
        public long Reward { get; set; }
        public bool IsEnding { get; set; }
    }

    public static readonly Dictionary<string, StoryChapter[]> Stories = new()
    {
        ["dragon"] =
        [
            // Chapter 0 - Start
            new() { Text = "You stand at the entrance of a dark cave. Smoke billows from within. A dragon lives here, and the village has asked you to deal with it.\n\n**A)** Enter the cave bravely\n**B)** Look for a back entrance\n**C)** Set a trap at the entrance", Choices = new() { ["A"] = 1, ["B"] = 2, ["C"] = 3 } },
            // Chapter 1 - Brave entrance
            new() { Text = "You march in with your torch held high. The dragon spots you immediately! Its eyes glow red.\n\n**A)** Fight the dragon head-on\n**B)** Try to reason with it\n**C)** Throw your torch and run", Choices = new() { ["A"] = 4, ["B"] = 5, ["C"] = 6 } },
            // Chapter 2 - Back entrance
            new() { Text = "You find a narrow tunnel behind the mountain. Inside, you see the dragon sleeping on a pile of gold!\n\n**A)** Steal some gold and sneak out\n**B)** Try to slay it in its sleep\n**C)** Block the tunnel to trap it", Choices = new() { ["A"] = 7, ["B"] = 8, ["C"] = 9 } },
            // Chapter 3 - Trap
            new() { Text = "You set up a clever net trap. Hours pass... the dragon emerges to hunt!\n\n**A)** Spring the trap\n**B)** Follow it to learn its patterns", Choices = new() { ["A"] = 10, ["B"] = 11 } },
            // Chapter 4 - Fight
            new() { Text = "The battle is fierce! Your sword strikes true, but the dragon's fire singes your armor. With one final blow, you slay the beast!\n\n🏆 **Quest Complete!** The village hails you as a hero!", Reward = 500, IsEnding = true },
            // Chapter 5 - Reason
            new() { Text = "The dragon pauses. 'A human who speaks rather than fights?' It turns out the dragon was only angry because miners disturbed its nest. You broker peace!\n\n🏆 **Quest Complete!** The village and dragon now live in harmony!", Reward = 750, IsEnding = true },
            // Chapter 6 - Run
            new() { Text = "The torch distracts the dragon momentarily, but it catches you at the exit. Your adventure ends in flames.\n\n💀 **Quest Failed!** Perhaps a braver approach next time.", Reward = 50, IsEnding = true },
            // Chapter 7 - Steal gold
            new() { Text = "You grab handfuls of gold coins and sneak out! The dragon never wakes. The village gets their peace, and you get rich!\n\n🏆 **Quest Complete!** Not the most honorable method, but effective!", Reward = 600, IsEnding = true },
            // Chapter 8 - Slay sleeping
            new() { Text = "Your blade finds the dragon's weak spot. It perishes without ever waking. The cave is yours!\n\n🏆 **Quest Complete!** The dragon menace is ended.", Reward = 400, IsEnding = true },
            // Chapter 9 - Block tunnel
            new() { Text = "You collapse the tunnel, trapping the dragon forever. But it finds another way out months later, angrier than ever...\n\n⚠️ **Quest Complete... for now.** The dragon will return.", Reward = 200, IsEnding = true },
            // Chapter 10 - Spring trap
            new() { Text = "The net falls on the dragon! It struggles but can't escape. The village captures it and sells it to a traveling circus.\n\n🏆 **Quest Complete!** Resourceful thinking!", Reward = 550, IsEnding = true },
            // Chapter 11 - Follow
            new() { Text = "You learn the dragon only attacks livestock. You help the village build stronger fences and the dragon finds food elsewhere.\n\n🏆 **Quest Complete!** A peaceful solution!", Reward = 650, IsEnding = true },
        ],
        ["treasure"] =
        [
            new() { Text = "A mysterious map leads to a treasure buried on Skull Island. Your ship arrives at the shore.\n\n**A)** Head straight to the X on the map\n**B)** Explore the beach first\n**C)** Climb the lookout hill", Choices = new() { ["A"] = 1, ["B"] = 2, ["C"] = 3 } },
            new() { Text = "The path is treacherous! You encounter quicksand.\n\n**A)** Jump over it\n**B)** Go around through the jungle", Choices = new() { ["A"] = 4, ["B"] = 5 } },
            new() { Text = "You find a shipwreck with useful supplies: rope, a compass, and a mysterious key.\n\n**A)** Take everything and head to X\n**B)** Investigate the shipwreck further", Choices = new() { ["A"] = 6, ["B"] = 7 } },
            new() { Text = "From the hill, you spot rivals heading for the treasure! You also see a shortcut.\n\n**A)** Take the shortcut to beat them\n**B)** Set up an ambush", Choices = new() { ["A"] = 8, ["B"] = 9 } },
            new() { Text = "You sink into the quicksand! Adventure over.\n\n💀 **Quest Failed!**", Reward = 25, IsEnding = true },
            new() { Text = "The jungle path leads to the treasure! You dig it up — a chest full of gold!\n\n🏆 **Quest Complete!**", Reward = 500, IsEnding = true },
            new() { Text = "With the supplies, you find the treasure easily. The key opens a secret compartment with even more loot!\n\n🏆 **Quest Complete!** Maximum treasure!", Reward = 800, IsEnding = true },
            new() { Text = "The shipwreck has a hidden cabin with a journal. It reveals the real treasure is in a sea cave!\n\n🏆 **Quest Complete!** You found the true treasure!", Reward = 1000, IsEnding = true },
            new() { Text = "The shortcut works! You reach the treasure first and escape by boat!\n\n🏆 **Quest Complete!** Speed wins!", Reward = 600, IsEnding = true },
            new() { Text = "Your ambush scares off the rivals. You claim the treasure at your leisure!\n\n🏆 **Quest Complete!** Strategic victory!", Reward = 700, IsEnding = true },
        ],
    };

    public static readonly string[] QuestIds = ["dragon", "treasure"];

    public async Task<(bool Success, string Message)> StartQuestAsync(ulong userId, string questId = null)
    {
        questId ??= QuestIds[new SantiRandom().Next(QuestIds.Length)];

        if (!Stories.ContainsKey(questId))
            return (false, $"Unknown quest. Available: {string.Join(", ", QuestIds)}");

        await using var ctx = _db.GetDbContext();

        // Check for active quest
        var active = await ctx.GetTable<StoryProgress>()
            .FirstOrDefaultAsyncLinqToDB(s => s.UserId == userId && !s.IsComplete);

        if (active is not null)
            return (false, "You have an active quest! Use `.quest continue <choice>` or `.quest abandon`.");

        await ctx.GetTable<StoryProgress>().InsertAsync(() => new StoryProgress
        {
            UserId = userId,
            QuestId = questId,
            Chapter = 0,
            ChoicePath = "",
            IsComplete = false,
            RewardsEarned = 0,
            DateAdded = DateTime.UtcNow
        });

        var chapter = Stories[questId][0];
        return (true, $"📖 **Quest: {questId.ToUpper()}**\n\n{chapter.Text}");
    }

    public async Task<(bool Success, string Message)> ContinueQuestAsync(ulong userId, string choice)
    {
        await using var ctx = _db.GetDbContext();
        var progress = await ctx.GetTable<StoryProgress>()
            .FirstOrDefaultAsyncLinqToDB(s => s.UserId == userId && !s.IsComplete);

        if (progress is null)
            return (false, "No active quest! Use `.quest start` to begin one.");

        if (!Stories.TryGetValue(progress.QuestId, out var chapters))
            return (false, "Quest data corrupted!");

        var current = chapters[progress.Chapter];
        choice = choice.ToUpper();

        if (current.Choices is null || !current.Choices.TryGetValue(choice, out var nextIdx))
            return (false, $"Invalid choice! Options: {string.Join(", ", current.Choices?.Keys ?? (IEnumerable<string>)Array.Empty<string>())}");

        var next = chapters[nextIdx];
        var newPath = string.IsNullOrEmpty(progress.ChoicePath) ? choice : $"{progress.ChoicePath},{choice}";

        if (next.IsEnding)
        {
            await ctx.GetTable<StoryProgress>()
                .Where(s => s.Id == progress.Id)
                .UpdateAsync(s => new StoryProgress
                {
                    Chapter = nextIdx,
                    ChoicePath = newPath,
                    IsComplete = true,
                    RewardsEarned = next.Reward
                });

            if (next.Reward > 0)
                await _cs.AddAsync(userId, next.Reward, new TxData("quest", "reward"));

            return (true, $"📖 {next.Text}\n\n💰 Reward: {next.Reward} 🥠");
        }

        await ctx.GetTable<StoryProgress>()
            .Where(s => s.Id == progress.Id)
            .UpdateAsync(s => new StoryProgress
            {
                Chapter = nextIdx,
                ChoicePath = newPath
            });

        return (true, $"📖 {next.Text}");
    }

    public async Task<(bool Success, string Message)> GetStatusAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var progress = await ctx.GetTable<StoryProgress>()
            .FirstOrDefaultAsyncLinqToDB(s => s.UserId == userId && !s.IsComplete);

        if (progress is null)
            return (false, "No active quest.");

        var chapter = Stories[progress.QuestId][progress.Chapter];
        return (true, $"📖 **Quest: {progress.QuestId.ToUpper()}** (Chapter {progress.Chapter})\nPath: {progress.ChoicePath}\n\n{chapter.Text}");
    }

    public async Task<(bool Success, string Message)> AbandonQuestAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<StoryProgress>()
            .DeleteAsync(s => s.UserId == userId && !s.IsComplete);

        return deleted > 0 ? (true, "Quest abandoned.") : (false, "No active quest.");
    }
}
