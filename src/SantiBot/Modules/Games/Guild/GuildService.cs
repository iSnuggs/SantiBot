#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games.Guild;

public sealed class GuildService(DbService _db, ICurrencyService _cs) : INService
{
    private static readonly SantiRandom _rng = new();

    // Level -> XP required to reach that level
    private static readonly Dictionary<int, long> _levelThresholds = new()
    {
        { 1, 0 },
        { 2, 1_000 },
        { 3, 3_000 },
        { 4, 7_000 },
        { 5, 15_000 },
        { 6, 30_000 },
        { 7, 55_000 },
        { 8, 100_000 },
        { 9, 175_000 },
        { 10, 300_000 },
    };

    // Level -> max members at that level
    private static readonly Dictionary<int, int> _levelMaxMembers = new()
    {
        { 1, 20 },
        { 2, 25 },
        { 3, 30 },
        { 4, 35 },
        { 5, 40 },
        { 6, 50 },
        { 7, 60 },
        { 8, 75 },
        { 9, 90 },
        { 10, 100 },
    };

    // Level -> perk description
    private static readonly Dictionary<int, string> _levelPerks = new()
    {
        { 1, "Base guild (20 members)" },
        { 2, "25 members, guild emoji in tag" },
        { 3, "30 members, +5% war score bonus" },
        { 4, "35 members, guild treasury interest (1%/day)" },
        { 5, "40 members, +10% war score bonus" },
        { 6, "50 members, reduced war cooldown" },
        { 7, "60 members, +15% war score bonus" },
        { 8, "75 members, double treasury interest" },
        { 9, "90 members, +20% war score bonus" },
        { 10, "100 members, legendary guild badge, +25% war score bonus" },
    };

    private const long GUILD_CREATE_COST = 5000;
    private const int WAR_DURATION_HOURS = 24;
    private const int WAR_BATTLE_COOLDOWN_SECONDS = 60;

    // Per-user war battle cooldown tracking
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong guildDiscordId, ulong userId), DateTime> _warBattleCooldowns = new();

    /// <summary>
    /// Create a new player guild. Costs 5000 currency.
    /// </summary>
    public async Task<(PlayerGuild guild, string error)> CreateGuildAsync(
        ulong guildDiscordId, ulong userId, string name, string tag, string emoji)
    {
        if (tag.Length < 3 || tag.Length > 5)
            return (null, "Tag must be 3-5 characters.");

        tag = tag.ToUpperInvariant();

        await using var ctx = _db.GetDbContext();

        // Check if user is already in a guild on this server
        var existingMember = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (existingMember is not null)
            return (null, "You are already in a guild. Leave your current guild first.");

        // Check for duplicate tag on this server
        var existingTag = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Tag == tag && x.GuildDiscordId == guildDiscordId);

        if (existingTag is not null)
            return (null, $"The tag `[{tag}]` is already taken on this server.");

        // Check for duplicate name on this server
        var existingName = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Name == name && x.GuildDiscordId == guildDiscordId);

        if (existingName is not null)
            return (null, $"A guild named **{name}** already exists on this server.");

        // Charge currency
        var taken = await _cs.RemoveAsync(userId, GUILD_CREATE_COST,
            new("guild", "create", $"Created guild [{tag}] {name}"));

        if (!taken)
            return (null, $"You need at least **{GUILD_CREATE_COST}** currency to create a guild.");

        var guild = new PlayerGuild
        {
            GuildDiscordId = guildDiscordId,
            Name = name,
            Tag = tag,
            Emoji = emoji ?? "⚔️",
            LeaderId = userId,
            Description = $"Welcome to {name}!",
        };

        ctx.Set<PlayerGuild>().Add(guild);
        await ctx.SaveChangesAsync();

        // Add leader as a member
        var member = new GuildMember
        {
            PlayerGuildId = guild.Id,
            UserId = userId,
            GuildDiscordId = guildDiscordId,
            Rank = "Leader",
        };

        ctx.Set<GuildMember>().Add(member);
        await ctx.SaveChangesAsync();

        return (guild, null);
    }

    /// <summary>
    /// Join an existing guild by tag.
    /// </summary>
    public async Task<(PlayerGuild guild, string error)> JoinGuildAsync(
        ulong guildDiscordId, ulong userId, string tag)
    {
        tag = tag.ToUpperInvariant();
        await using var ctx = _db.GetDbContext();

        // Check if already in a guild
        var existingMember = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (existingMember is not null)
            return (null, "You are already in a guild. Leave first with `.guild leave`.");

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Tag == tag && x.GuildDiscordId == guildDiscordId);

        if (guild is null)
            return (null, $"No guild with tag `[{tag}]` found.");

        if (!guild.IsRecruiting)
            return (null, "That guild is not currently recruiting.");

        if (guild.MemberCount >= guild.MaxMembers)
            return (null, "That guild is full.");

        var member = new GuildMember
        {
            PlayerGuildId = guild.Id,
            UserId = userId,
            GuildDiscordId = guildDiscordId,
            Rank = "Recruit",
        };

        ctx.Set<GuildMember>().Add(member);

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == guild.Id)
            .UpdateAsync(_ => new PlayerGuild { MemberCount = guild.MemberCount + 1 });

        await ctx.SaveChangesAsync();
        guild.MemberCount += 1;
        return (guild, null);
    }

    /// <summary>
    /// Leave your current guild. Leaders must transfer ownership first.
    /// </summary>
    public async Task<(string guildName, string error)> LeaveGuildAsync(ulong guildDiscordId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (null, "You are not in a guild.");

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == member.PlayerGuildId);

        if (guild is null)
            return (null, "Guild not found.");

        if (member.Rank == "Leader")
            return (null, "Leaders cannot leave. Promote someone else to Leader first, or disband the guild.");

        await ctx.GetTable<GuildMember>()
            .Where(x => x.Id == member.Id)
            .DeleteAsync();

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == guild.Id)
            .UpdateAsync(_ => new PlayerGuild { MemberCount = guild.MemberCount - 1 });

        return (guild.Name, null);
    }

    /// <summary>
    /// Kick a member from the guild. Requires Officer+ rank.
    /// </summary>
    public async Task<(string kickedName, string error)> KickMemberAsync(
        ulong guildDiscordId, ulong kickerId, ulong targetId)
    {
        await using var ctx = _db.GetDbContext();

        var kicker = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == kickerId && x.GuildDiscordId == guildDiscordId);

        if (kicker is null)
            return (null, "You are not in a guild.");

        if (kicker.Rank != "Leader" && kicker.Rank != "Officer")
            return (null, "Only Leaders and Officers can kick members.");

        var target = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == targetId && x.GuildDiscordId == guildDiscordId
                && x.PlayerGuildId == kicker.PlayerGuildId);

        if (target is null)
            return (null, "That user is not in your guild.");

        if (target.Rank == "Leader")
            return (null, "You cannot kick the guild leader.");

        if (kicker.Rank == "Officer" && target.Rank == "Officer")
            return (null, "Officers cannot kick other Officers.");

        await ctx.GetTable<GuildMember>()
            .Where(x => x.Id == target.Id)
            .DeleteAsync();

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == kicker.PlayerGuildId);

        if (guild is not null)
        {
            await ctx.GetTable<PlayerGuild>()
                .Where(x => x.Id == guild.Id)
                .UpdateAsync(_ => new PlayerGuild { MemberCount = guild.MemberCount - 1 });
        }

        return (targetId.ToString(), null);
    }

    /// <summary>
    /// Promote a member. Leader->Officer->Member->Recruit path upward.
    /// Only Leaders can promote to Officer. Officers can promote Recruits to Members.
    /// </summary>
    public async Task<(string newRank, string error)> PromoteMemberAsync(
        ulong guildDiscordId, ulong promoterId, ulong targetId)
    {
        await using var ctx = _db.GetDbContext();

        var promoter = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == promoterId && x.GuildDiscordId == guildDiscordId);

        if (promoter is null)
            return (null, "You are not in a guild.");

        var target = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == targetId && x.GuildDiscordId == guildDiscordId
                && x.PlayerGuildId == promoter.PlayerGuildId);

        if (target is null)
            return (null, "That user is not in your guild.");

        var newRank = target.Rank switch
        {
            "Recruit" when promoter.Rank is "Leader" or "Officer" => "Member",
            "Member" when promoter.Rank == "Leader" => "Officer",
            "Officer" when promoter.Rank == "Leader" => "Leader",
            _ => null,
        };

        if (newRank is null)
            return (null, "You don't have permission to promote this member further.");

        // If promoting someone to Leader, demote yourself to Officer
        if (newRank == "Leader")
        {
            await ctx.GetTable<GuildMember>()
                .Where(x => x.Id == promoter.Id)
                .UpdateAsync(_ => new GuildMember { Rank = "Officer" });

            await ctx.GetTable<PlayerGuild>()
                .Where(x => x.Id == promoter.PlayerGuildId)
                .UpdateAsync(_ => new PlayerGuild { LeaderId = targetId });
        }

        await ctx.GetTable<GuildMember>()
            .Where(x => x.Id == target.Id)
            .UpdateAsync(_ => new GuildMember { Rank = newRank });

        return (newRank, null);
    }

    /// <summary>
    /// Demote a member. Leader can demote Officers to Members. Officers can demote Members to Recruits.
    /// </summary>
    public async Task<(string newRank, string error)> DemoteMemberAsync(
        ulong guildDiscordId, ulong demoterId, ulong targetId)
    {
        await using var ctx = _db.GetDbContext();

        var demoter = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == demoterId && x.GuildDiscordId == guildDiscordId);

        if (demoter is null)
            return (null, "You are not in a guild.");

        var target = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == targetId && x.GuildDiscordId == guildDiscordId
                && x.PlayerGuildId == demoter.PlayerGuildId);

        if (target is null)
            return (null, "That user is not in your guild.");

        var newRank = target.Rank switch
        {
            "Officer" when demoter.Rank == "Leader" => "Member",
            "Member" when demoter.Rank is "Leader" or "Officer" => "Recruit",
            _ => null,
        };

        if (newRank is null)
            return (null, "You don't have permission to demote this member.");

        await ctx.GetTable<GuildMember>()
            .Where(x => x.Id == target.Id)
            .UpdateAsync(_ => new GuildMember { Rank = newRank });

        return (newRank, null);
    }

    /// <summary>
    /// Donate currency to the guild treasury. Grants guild XP equal to amount donated.
    /// </summary>
    public async Task<(long newTreasury, string error)> ContributeAsync(
        ulong guildDiscordId, ulong userId, long amount)
    {
        if (amount <= 0)
            return (0, "Amount must be positive.");

        await using var ctx = _db.GetDbContext();

        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (0, "You are not in a guild.");

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == member.PlayerGuildId);

        if (guild is null)
            return (0, "Guild not found.");

        var taken = await _cs.RemoveAsync(userId, amount,
            new("guild", "contribute", $"Donated to [{guild.Tag}] {guild.Name}"));

        if (!taken)
            return (0, "You don't have enough currency.");

        var newTreasury = guild.Treasury + amount;
        var newXp = guild.Xp + amount;

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == guild.Id)
            .UpdateAsync(_ => new PlayerGuild
            {
                Treasury = newTreasury,
                Xp = newXp,
            });

        // Update member contribution stats
        await ctx.GetTable<GuildMember>()
            .Where(x => x.Id == member.Id)
            .UpdateAsync(_ => new GuildMember
            {
                ContributedCurrency = member.ContributedCurrency + amount,
                ContributedXp = member.ContributedXp + amount,
            });

        // Check for level up
        await TryLevelUpAsync(ctx, guild.Id, newXp);

        return (newTreasury, null);
    }

    /// <summary>
    /// Check if the guild qualifies for a level up and apply it.
    /// </summary>
    private async Task TryLevelUpAsync(SantiContext ctx, int guildId, long currentXp)
    {
        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == guildId);

        if (guild is null || guild.Level >= 10)
            return;

        var nextLevel = guild.Level + 1;
        if (!_levelThresholds.TryGetValue(nextLevel, out var threshold))
            return;

        if (currentXp < threshold)
            return;

        var newMaxMembers = _levelMaxMembers.GetValueOrDefault(nextLevel, guild.MaxMembers);

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == guildId)
            .UpdateAsync(_ => new PlayerGuild
            {
                Level = nextLevel,
                MaxMembers = newMaxMembers,
            });
    }

    /// <summary>
    /// Get guild info by tag.
    /// </summary>
    public async Task<PlayerGuild> GetGuildAsync(ulong guildDiscordId, string tag)
    {
        tag = tag.ToUpperInvariant();
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Tag == tag && x.GuildDiscordId == guildDiscordId);
    }

    /// <summary>
    /// Get the guild a user belongs to.
    /// </summary>
    public async Task<(PlayerGuild guild, GuildMember member)> GetUserGuildAsync(ulong guildDiscordId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (null, null);

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == member.PlayerGuildId);

        return (guild, member);
    }

    /// <summary>
    /// Get all members of a guild.
    /// </summary>
    public async Task<List<GuildMember>> GetGuildMembersAsync(int guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<GuildMember>()
            .Where(x => x.PlayerGuildId == guildId)
            .OrderByDescending(x => x.Rank == "Leader" ? 4
                : x.Rank == "Officer" ? 3
                : x.Rank == "Member" ? 2
                : 1)
            .ThenByDescending(x => x.ContributedCurrency)
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Get the top guilds on this server by level and XP.
    /// </summary>
    public async Task<List<PlayerGuild>> GetGuildLeaderboardAsync(ulong guildDiscordId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PlayerGuild>()
            .Where(x => x.GuildDiscordId == guildDiscordId)
            .OrderByDescending(x => x.Level)
            .ThenByDescending(x => x.Xp)
            .Take(count)
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Declare war on another guild. Only Leaders can declare war.
    /// Wars last 24 hours by default.
    /// </summary>
    public async Task<(GuildWar war, string error)> DeclareWarAsync(
        ulong guildDiscordId, ulong userId, string targetTag)
    {
        targetTag = targetTag.ToUpperInvariant();
        await using var ctx = _db.GetDbContext();

        var attackerMember = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (attackerMember is null)
            return (null, "You are not in a guild.");

        if (attackerMember.Rank != "Leader")
            return (null, "Only the guild Leader can declare war.");

        var attackerGuild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == attackerMember.PlayerGuildId);

        var defenderGuild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Tag == targetTag && x.GuildDiscordId == guildDiscordId);

        if (defenderGuild is null)
            return (null, $"No guild with tag `[{targetTag}]` found.");

        if (attackerGuild.Id == defenderGuild.Id)
            return (null, "You can't declare war on your own guild.");

        // Check for existing active war between these guilds
        var existingWar = await ctx.GetTable<GuildWar>()
            .FirstOrDefaultAsync(x => x.GuildDiscordId == guildDiscordId
                && x.Status != "Completed"
                && ((x.AttackerGuildId == attackerGuild.Id && x.DefenderGuildId == defenderGuild.Id)
                    || (x.AttackerGuildId == defenderGuild.Id && x.DefenderGuildId == attackerGuild.Id)));

        if (existingWar is not null)
            return (null, "There is already an active war between these guilds.");

        // War costs 1000 from treasury
        if (attackerGuild.Treasury < 1000)
            return (null, "Your guild needs at least **1,000** in the treasury to declare war.");

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == attackerGuild.Id)
            .UpdateAsync(_ => new PlayerGuild { Treasury = attackerGuild.Treasury - 1000 });

        var war = new GuildWar
        {
            AttackerGuildId = attackerGuild.Id,
            DefenderGuildId = defenderGuild.Id,
            GuildDiscordId = guildDiscordId,
            Status = "Active",
            StartedAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(WAR_DURATION_HOURS),
        };

        ctx.Set<GuildWar>().Add(war);
        await ctx.SaveChangesAsync();

        return (war, null);
    }

    /// <summary>
    /// Contribute a battle win to your guild's war score.
    /// Each battle gives 1-3 points based on RNG.
    /// </summary>
    public async Task<(int pointsGained, int totalScore, string enemyTag, string error)> WarBattleAsync(
        ulong guildDiscordId, ulong userId)
    {
        // Cooldown check (60 seconds between battles)
        var key = (guildDiscordId, userId);
        if (_warBattleCooldowns.TryGetValue(key, out var lastBattle))
        {
            var cooldownEnd = lastBattle.AddSeconds(WAR_BATTLE_COOLDOWN_SECONDS);
            if (DateTime.UtcNow < cooldownEnd)
            {
                var remaining = cooldownEnd - DateTime.UtcNow;
                return (0, 0, null, $"You're recovering from battle! Try again in **{remaining.Seconds}s**.");
            }
        }

        await using var ctx = _db.GetDbContext();

        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (0, 0, null, "You are not in a guild.");

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == member.PlayerGuildId);

        // Find an active war involving this guild
        var war = await ctx.GetTable<GuildWar>()
            .FirstOrDefaultAsync(x => x.GuildDiscordId == guildDiscordId
                && x.Status == "Active"
                && (x.AttackerGuildId == guild.Id || x.DefenderGuildId == guild.Id));

        if (war is null)
            return (0, 0, null, "Your guild is not currently at war.");

        if (DateTime.UtcNow > war.EndsAt)
        {
            // War expired, end it
            await EndWarInternalAsync(ctx, war);
            return (0, 0, null, "The war has ended! Check `.guild warstatus` for results.");
        }

        // War score bonus based on guild level
        var levelBonus = guild.Level switch
        {
            >= 10 => 1.25,
            >= 9 => 1.20,
            >= 7 => 1.15,
            >= 5 => 1.10,
            >= 3 => 1.05,
            _ => 1.0,
        };

        var basePoints = _rng.Next(1, 4);
        var points = (int)(basePoints * levelBonus);

        // Record cooldown
        _warBattleCooldowns[key] = DateTime.UtcNow;

        var isAttacker = war.AttackerGuildId == guild.Id;
        string enemyTag;

        if (isAttacker)
        {
            var newScore = war.AttackerScore + points;
            await ctx.GetTable<GuildWar>()
                .Where(x => x.Id == war.Id)
                .UpdateAsync(_ => new GuildWar { AttackerScore = newScore });

            var enemy = await ctx.GetTable<PlayerGuild>()
                .FirstOrDefaultAsync(x => x.Id == war.DefenderGuildId);
            enemyTag = enemy?.Tag ?? "???";
            return (points, newScore, enemyTag, null);
        }
        else
        {
            var newScore = war.DefenderScore + points;
            await ctx.GetTable<GuildWar>()
                .Where(x => x.Id == war.Id)
                .UpdateAsync(_ => new GuildWar { DefenderScore = newScore });

            var enemy = await ctx.GetTable<PlayerGuild>()
                .FirstOrDefaultAsync(x => x.Id == war.AttackerGuildId);
            enemyTag = enemy?.Tag ?? "???";
            return (points, newScore, enemyTag, null);
        }
    }

    /// <summary>
    /// End a war and distribute rewards. Can be called manually by a leader or when the timer expires.
    /// </summary>
    public async Task<(string winnerTag, string loserTag, long reward, string error)> EndWarAsync(
        ulong guildDiscordId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (null, null, 0, "You are not in a guild.");

        if (member.Rank != "Leader")
            return (null, null, 0, "Only the guild Leader can end a war.");

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == member.PlayerGuildId);

        var war = await ctx.GetTable<GuildWar>()
            .FirstOrDefaultAsync(x => x.GuildDiscordId == guildDiscordId
                && x.Status == "Active"
                && (x.AttackerGuildId == guild.Id || x.DefenderGuildId == guild.Id));

        if (war is null)
            return (null, null, 0, "Your guild is not currently at war.");

        if (DateTime.UtcNow < war.EndsAt)
            return (null, null, 0, $"The war hasn't ended yet. It ends <t:{new DateTimeOffset(war.EndsAt).ToUnixTimeSeconds()}:R>.");

        return await EndWarInternalAsync(ctx, war);
    }

    /// <summary>
    /// Get the current war status for a user's guild.
    /// </summary>
    public async Task<(GuildWar war, PlayerGuild attacker, PlayerGuild defender, string error)> GetWarStatusAsync(
        ulong guildDiscordId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (null, null, null, "You are not in a guild.");

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == member.PlayerGuildId);

        var war = await ctx.GetTable<GuildWar>()
            .FirstOrDefaultAsync(x => x.GuildDiscordId == guildDiscordId
                && x.Status == "Active"
                && (x.AttackerGuildId == guild.Id || x.DefenderGuildId == guild.Id));

        if (war is null)
            return (null, null, null, "Your guild is not currently at war.");

        var attacker = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == war.AttackerGuildId);

        var defender = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == war.DefenderGuildId);

        return (war, attacker, defender, null);
    }

    /// <summary>
    /// Toggle recruiting status for your guild.
    /// </summary>
    public async Task<(bool newStatus, string error)> SetRecruitingAsync(
        ulong guildDiscordId, ulong userId, bool recruiting)
    {
        await using var ctx = _db.GetDbContext();

        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (false, "You are not in a guild.");

        if (member.Rank != "Leader" && member.Rank != "Officer")
            return (false, "Only Leaders and Officers can change recruiting status.");

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == member.PlayerGuildId)
            .UpdateAsync(_ => new PlayerGuild { IsRecruiting = recruiting });

        return (recruiting, null);
    }

    /// <summary>
    /// Set the guild description. Leaders and Officers only.
    /// </summary>
    public async Task<(string guildName, string error)> SetDescriptionAsync(
        ulong guildDiscordId, ulong userId, string description)
    {
        if (description.Length > 200)
            return (null, "Description must be 200 characters or fewer.");

        await using var ctx = _db.GetDbContext();

        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (null, "You are not in a guild.");

        if (member.Rank != "Leader" && member.Rank != "Officer")
            return (null, "Only Leaders and Officers can change the description.");

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == member.PlayerGuildId);

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == member.PlayerGuildId)
            .UpdateAsync(_ => new PlayerGuild { Description = description });

        return (guild?.Name ?? "your guild", null);
    }

    /// <summary>
    /// Disband a guild. Only the Leader can disband. Returns treasury to leader.
    /// </summary>
    public async Task<(string guildName, long treasury, string error)> DisbandGuildAsync(
        ulong guildDiscordId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var member = await ctx.GetTable<GuildMember>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildDiscordId == guildDiscordId);

        if (member is null)
            return (null, 0, "You are not in a guild.");

        if (member.Rank != "Leader")
            return (null, 0, "Only the guild Leader can disband the guild.");

        var guild = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == member.PlayerGuildId);

        if (guild is null)
            return (null, 0, "Guild not found.");

        // Check for active wars
        var activeWar = await ctx.GetTable<GuildWar>()
            .FirstOrDefaultAsync(x => x.Status == "Active"
                && (x.AttackerGuildId == guild.Id || x.DefenderGuildId == guild.Id));

        if (activeWar is not null)
            return (null, 0, "Cannot disband while in an active war.");

        // Return treasury to leader
        if (guild.Treasury > 0)
        {
            await _cs.AddAsync(userId, guild.Treasury,
                new("guild", "disband", $"Treasury returned from [{guild.Tag}] {guild.Name}"));
        }

        // Delete all members
        await ctx.GetTable<GuildMember>()
            .Where(x => x.PlayerGuildId == guild.Id)
            .DeleteAsync();

        // Delete the guild
        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == guild.Id)
            .DeleteAsync();

        return (guild.Name, guild.Treasury, null);
    }

    /// <summary>
    /// Get the level perk description.
    /// </summary>
    public static string GetLevelPerk(int level)
        => _levelPerks.GetValueOrDefault(level, "Unknown");

    /// <summary>
    /// Get XP needed for next level.
    /// </summary>
    public static long GetXpForNextLevel(int currentLevel)
    {
        var nextLevel = currentLevel + 1;
        return _levelThresholds.GetValueOrDefault(nextLevel, -1);
    }

    private async Task<(string winnerTag, string loserTag, long reward, string error)> EndWarInternalAsync(
        SantiContext ctx, GuildWar war)
    {
        var attacker = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == war.AttackerGuildId);

        var defender = await ctx.GetTable<PlayerGuild>()
            .FirstOrDefaultAsync(x => x.Id == war.DefenderGuildId);

        await ctx.GetTable<GuildWar>()
            .Where(x => x.Id == war.Id)
            .UpdateAsync(_ => new GuildWar { Status = "Completed" });

        var attackerWins = war.AttackerScore > war.DefenderScore;
        var isDraw = war.AttackerScore == war.DefenderScore;

        if (isDraw)
        {
            // Draw - no rewards, just XP for both
            await ctx.GetTable<PlayerGuild>()
                .Where(x => x.Id == attacker.Id)
                .UpdateAsync(_ => new PlayerGuild { Xp = attacker.Xp + 500 });

            await ctx.GetTable<PlayerGuild>()
                .Where(x => x.Id == defender.Id)
                .UpdateAsync(_ => new PlayerGuild { Xp = defender.Xp + 500 });

            return (null, null, 0, "draw");
        }

        var winner = attackerWins ? attacker : defender;
        var loser = attackerWins ? defender : attacker;

        // Winner gets 2000 XP and reward currency distributed to members
        long warReward = 500 * winner.MemberCount;
        var xpReward = 2000L;
        var loserXp = 500L;

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == winner.Id)
            .UpdateAsync(_ => new PlayerGuild { Xp = winner.Xp + xpReward });

        await ctx.GetTable<PlayerGuild>()
            .Where(x => x.Id == loser.Id)
            .UpdateAsync(_ => new PlayerGuild { Xp = loser.Xp + loserXp });

        // Distribute currency reward to winning guild members
        var winnerMembers = await ctx.GetTable<GuildMember>()
            .Where(x => x.PlayerGuildId == winner.Id)
            .ToListAsyncLinqToDB();

        var perMemberReward = warReward / Math.Max(winnerMembers.Count, 1);
        foreach (var m in winnerMembers)
        {
            await _cs.AddAsync(m.UserId, perMemberReward,
                new("guild_war", "win", $"War victory reward [{winner.Tag}]"));
        }

        // Level up check for winner
        await TryLevelUpAsync(ctx, winner.Id, winner.Xp + xpReward);

        return (winner.Tag, loser.Tag, perMemberReward, null);
    }
}
