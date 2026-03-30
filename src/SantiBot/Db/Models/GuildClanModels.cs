#nullable disable

namespace SantiBot.Db.Models;

public class PlayerGuild : DbEntity
{
    public ulong GuildDiscordId { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public string Description { get; set; }
    public string Emoji { get; set; }
    public int Level { get; set; } = 1;
    public long Xp { get; set; }
    public ulong LeaderId { get; set; }
    public long Treasury { get; set; }
    public int MemberCount { get; set; } = 1;
    public int MaxMembers { get; set; } = 20;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRecruiting { get; set; } = true;
}

public class GuildMember : DbEntity
{
    public int PlayerGuildId { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildDiscordId { get; set; }
    public string Rank { get; set; } = "Recruit";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public long ContributedCurrency { get; set; }
    public long ContributedXp { get; set; }
}

public class GuildWar : DbEntity
{
    public int AttackerGuildId { get; set; }
    public int DefenderGuildId { get; set; }
    public ulong GuildDiscordId { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttackerScore { get; set; }
    public int DefenderScore { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime EndsAt { get; set; }
}

public class TradeOffer : DbEntity
{
    public ulong SellerUserId { get; set; }
    public ulong GuildDiscordId { get; set; }
    public string ItemName { get; set; }
    public string ItemType { get; set; }
    public int Quantity { get; set; }
    public long PricePerUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ListedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

public class TradeTransaction : DbEntity
{
    public ulong BuyerUserId { get; set; }
    public ulong SellerUserId { get; set; }
    public ulong GuildDiscordId { get; set; }
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public long TotalPrice { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
