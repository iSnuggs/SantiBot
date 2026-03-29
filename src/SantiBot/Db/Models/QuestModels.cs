#nullable disable
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

// ═══════════════════════════════════════════════════════════
//  QUEST PROGRESS — tracks each active/completed quest
// ═══════════════════════════════════════════════════════════
public class QuestProgress : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    /// <summary>Unique quest template identifier (e.g. "daily_send_messages", "story_the_awakening")</summary>
    public string QuestId { get; set; }

    public string QuestName { get; set; }
    public string Description { get; set; }

    /// <summary>"Daily", "Weekly", "Story", "Side"</summary>
    public string Type { get; set; }

    /// <summary>"Active", "Completed", "Failed"</summary>
    public string Status { get; set; }

    public int CurrentProgress { get; set; }
    public int RequiredProgress { get; set; }

    public long XpReward { get; set; }
    public long CurrencyReward { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public sealed class QuestProgressEntityConfiguration : IEntityTypeConfiguration<QuestProgress>
{
    public void Configure(EntityTypeBuilder<QuestProgress> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.GuildId });
        builder.HasIndex(x => new { x.UserId, x.GuildId, x.QuestId }).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}

// ═══════════════════════════════════════════════════════════
//  QUEST LOG — lifetime stats per user per guild
// ═══════════════════════════════════════════════════════════
public class QuestLog : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    public int TotalQuestsCompleted { get; set; }
    public int DailyQuestsCompleted { get; set; }
    public int WeeklyQuestsCompleted { get; set; }
    public int StoryQuestsCompleted { get; set; }

    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    public DateTime LastDailyRefresh { get; set; }
    public DateTime LastWeeklyRefresh { get; set; }
}

public sealed class QuestLogEntityConfiguration : IEntityTypeConfiguration<QuestLog>
{
    public void Configure(EntityTypeBuilder<QuestLog> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.GuildId }).IsUnique();
    }
}

// ═══════════════════════════════════════════════════════════
//  FACTION STANDING — reputation with each faction
// ═══════════════════════════════════════════════════════════
public class FactionStanding : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    public string FactionName { get; set; }

    public int Reputation { get; set; }

    /// <summary>"Outsider", "Initiate", "Member", "Veteran", "Champion", "Legend"</summary>
    public string Rank { get; set; }
}

public sealed class FactionStandingEntityConfiguration : IEntityTypeConfiguration<FactionStanding>
{
    public void Configure(EntityTypeBuilder<FactionStanding> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.GuildId, x.FactionName }).IsUnique();
    }
}
