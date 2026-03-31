#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SantiBot.Db.Models;
using SantiBot.Modules.Waifus.Waifu.Db;
using SantiBot.Modules.Utility.Premium;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SantiBot.Db;

public abstract class SantiContext : DbContext
{
    public DbSet<GuildConfig> GuildConfigs { get; set; }
    public DbSet<Permissionv2> Permissions { get; set; }

    //new
    public DbSet<XpSettings> XpSettings { get; set; }

    public DbSet<GreetSettings> GreetSettings { get; set; }

    public DbSet<Quote> Quotes { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<MusicPlaylist> MusicPlaylists { get; set; }
    public DbSet<SantiExpression> Expressions { get; set; }
    public DbSet<CurrencyTransaction> CurrencyTransactions { get; set; }
    public DbSet<Warning> Warnings { get; set; }
    public DbSet<UserXpStats> UserXpStats { get; set; }
    public DbSet<ClubInfo> Clubs { get; set; }
    public DbSet<ClubBans> ClubBans { get; set; }
    public DbSet<ClubApplicants> ClubApplicants { get; set; }


    //logging
    public DbSet<LogSetting> LogSettings { get; set; }
    public DbSet<IgnoredLogItem> IgnoredLogChannels { get; set; }

    public DbSet<RotatingPlayingStatus> RotatingStatus { get; set; }
    public DbSet<BlacklistEntry> Blacklist { get; set; }
    public DbSet<AutoCommand> AutoCommands { get; set; }
    public DbSet<RewardedUser> RewardedUsers { get; set; }
    public DbSet<PlantedCurrency> PlantedCurrency { get; set; }
    public DbSet<BanTemplate> BanTemplates { get; set; }
    public DbSet<DiscordPermOverride> DiscordPermOverrides { get; set; }
    public DbSet<DiscordUser> DiscordUser { get; set; }
    public DbSet<MusicPlayerSettings> MusicPlayerSettings { get; set; }
    public DbSet<Repeater> Repeaters { get; set; }
    public DbSet<WaifuInfo> WaifuInfo { get; set; }
    public DbSet<ImageOnlyChannel> ImageOnlyChannels { get; set; }
    public DbSet<AutoTranslateChannel> AutoTranslateChannels { get; set; }
    public DbSet<AutoTranslateUser> AutoTranslateUsers { get; set; }


    public DbSet<BankUser> BankUsers { get; set; }

    public DbSet<ReactionRoleV2> ReactionRoles { get; set; }

    public DbSet<PatronUser> Patrons { get; set; }

    public DbSet<StreamOnlineMessage> StreamOnlineMessages { get; set; }

    public DbSet<StickyRole> StickyRoles { get; set; }

    public DbSet<TodoModel> Todos { get; set; }
    public DbSet<ArchivedTodoListModel> TodosArchive { get; set; }
    public DbSet<HoneypotChannel> HoneyPotChannels { get; set; }

    // SantiBot additions
    public DbSet<UserXpCard> UserXpCards { get; set; }
    public DbSet<StarboardEntry> StarboardEntries { get; set; }
    public DbSet<StarboardSettings> StarboardSettings { get; set; }
    public DbSet<PollModel> Polls { get; set; }
    public DbSet<PollVote> PollVotes { get; set; }
    public DbSet<SuggestionModel> Suggestions { get; set; }
    public DbSet<FormModel> Forms { get; set; }
    public DbSet<FormResponse> FormResponses { get; set; }
    public DbSet<AutoPurgeConfig> AutoPurgeConfigs { get; set; }
    public DbSet<SavedEmbed> SavedEmbeds { get; set; }
    public DbSet<AutomodRule> AutomodRules { get; set; }
    public DbSet<AutomodRuleExemption> AutomodRuleExemptions { get; set; }
    public DbSet<AutomodInfraction> AutomodInfractions { get; set; }
    public DbSet<AutoResponse> AutoResponses { get; set; }
    public DbSet<ModCase> ModCases { get; set; }
    public DbSet<ModNote> ModNotes { get; set; }
    public DbSet<AutoPunishConfig> AutoPunishConfigs { get; set; }
    public DbSet<ModSettings> ModSettings { get; set; }
    public DbSet<TicketConfig> TicketConfigs { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<AutobanRule> AutobanRules { get; set; }
    public DbSet<AutoDeleteRule> AutoDeleteRules { get; set; }
    public DbSet<ScheduledMessage> ScheduledMessages { get; set; }
    public DbSet<VerificationGate> VerificationGates { get; set; }
    public DbSet<VoiceTextLink> VoiceTextLinks { get; set; }
    public DbSet<DehoistConfig> DehoistConfigs { get; set; }
    public DbSet<WelcomeImageConfig> WelcomeImageConfigs { get; set; }
    public DbSet<TikTokFollow> TikTokFollows { get; set; }
    public DbSet<YouTubeFeedSub> YouTubeFeedSubs { get; set; }
    public DbSet<KickStreamFollow> KickStreamFollows { get; set; }
    public DbSet<BlueskyFeedSub> BlueskyFeedSubs { get; set; }
    public DbSet<ModMailConfig> ModMailConfigs { get; set; }
    public DbSet<ModMailThread> ModMailThreads { get; set; }
    public DbSet<ModMailMessage> ModMailMessages { get; set; }
    public DbSet<ModMailBlock> ModMailBlocks { get; set; }

    // 51 new features (bonus batch)
    public DbSet<AfkUser> AfkUsers { get; set; }
    public DbSet<AfkVoiceKickConfig> AfkVoiceKickConfigs { get; set; }
    public DbSet<AltDetectConfig> AltDetectConfigs { get; set; }
    public DbSet<BanAppeal> BanAppeals { get; set; }
    public DbSet<BanAppealConfig> BanAppealConfigs { get; set; }
    public DbSet<BirthdayConfig> BirthdayConfigs { get; set; }
    public DbSet<BumpReminderConfig> BumpReminderConfigs { get; set; }
    public DbSet<CommandUsage> CommandUsages { get; set; }
    public DbSet<ConfessionConfig> ConfessionConfigs { get; set; }
    public DbSet<CountingConfig> CountingConfigs { get; set; }
    public DbSet<CraftingInventory> CraftingInventories { get; set; }
    public DbSet<DashboardAuditLog> DashboardAuditLogs { get; set; }
    public DbSet<GameServerWatch> GameServerWatches { get; set; }
    public DbSet<GitHubRepoWatch> GitHubRepoWatches { get; set; }
    public DbSet<HeistSession> HeistSessions { get; set; }
    public DbSet<InviteTrackConfig> InviteTrackConfigs { get; set; }
    public DbSet<JoinSoundConfig> JoinSoundConfigs { get; set; }
    public DbSet<KnownAlt> KnownAlts { get; set; }
    public DbSet<MessageStat> MessageStats { get; set; }
    public DbSet<MilestoneConfig> MilestoneConfigs { get; set; }
    public DbSet<ModAction> ModActions { get; set; }
    public DbSet<PhishingConfig> PhishingConfigs { get; set; }
    public DbSet<QuarantineConfig> QuarantineConfigs { get; set; }
    public DbSet<RaidScoreConfig> RaidScoreConfigs { get; set; }
    public DbSet<RepLog> RepLogs { get; set; }
    public DbSet<ScheduledEvent> ScheduledEvents { get; set; }
    public DbSet<ScheduledRoleGrant> ScheduledRoleGrants { get; set; }
    public DbSet<ScheduledTimeout> ScheduledTimeouts { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<StockHolding> StockHoldings { get; set; }
    public DbSet<ThreadArchiveConfig> ThreadArchiveConfigs { get; set; }
    public DbSet<TrackedInvite> TrackedInvites { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<UserBirthday> UserBirthdays { get; set; }
    public DbSet<UserJoinSound> UserJoinSounds { get; set; }
    public DbSet<UserReputation> UserReputations { get; set; }
    public DbSet<WarnDecayConfig> WarnDecayConfigs { get; set; }
    public DbSet<WebhookRelayConfig> WebhookRelayConfigs { get; set; }
    public DbSet<WelcomeQuizConfig> WelcomeQuizConfigs { get; set; }
    public DbSet<DailyStreak> DailyStreaks { get; set; }
    public DbSet<FishingRod> FishingRods { get; set; }
    public DbSet<ReactionResponse> ReactionResponses { get; set; }
    public DbSet<ShopListing> ShopListings { get; set; }
    public DbSet<ShopPurchase> ShopPurchases { get; set; }
    public DbSet<StickyMessage> StickyMessages { get; set; }
    public DbSet<UserPet> UserPets { get; set; }
    public DbSet<Pet> Pets { get; set; }

    // public DbSet<GuildColors> GuildColors { get; set; }

    // ==========================================
    // Phase 9 — Moderation & Server Management
    // ==========================================
    public DbSet<RegexAutomodRule> RegexAutomodRules { get; set; }
    public DbSet<SlowmodeSchedule> SlowmodeSchedules { get; set; }
    public DbSet<WarningPoint> WarningPoints { get; set; }
    public DbSet<WarningPointConfig> WarningPointConfigs { get; set; }
    public DbSet<LockdownPreset> LockdownPresets { get; set; }
    public DbSet<InviteWhitelist> InviteWhitelists { get; set; }
    public DbSet<InviteWhitelistConfig> InviteWhitelistConfigs { get; set; }
    public DbSet<ModTranslateConfig> ModTranslateConfigs { get; set; }
    public DbSet<ModShift> ModShifts { get; set; }
    public DbSet<EvidenceItem> EvidenceItems { get; set; }
    public DbSet<UserNote> UserNotes { get; set; }
    public DbSet<BanSyncConfig> BanSyncConfigs { get; set; }
    public DbSet<BanSyncEntry> BanSyncEntries { get; set; }
    public DbSet<SmartSpamConfig> SmartSpamConfigs { get; set; }
    public DbSet<ContentAgeGate> ContentAgeGates { get; set; }
    public DbSet<ModActionTemplate> ModActionTemplates { get; set; }
    public DbSet<DropdownRolePanel> DropdownRolePanels { get; set; }
    public DbSet<DropdownRoleOption> DropdownRoleOptions { get; set; }
    public DbSet<ChannelTemplate> ChannelTemplates { get; set; }
    public DbSet<AutoArchiveConfig> AutoArchiveConfigs { get; set; }
    public DbSet<AutoArchiveExclusion> AutoArchiveExclusions { get; set; }
    public DbSet<ServerBackup> ServerBackups { get; set; }
    public DbSet<ChannelActivity> ChannelActivities { get; set; }
    public DbSet<ChannelPrefix> ChannelPrefixes { get; set; }
    public DbSet<HealthReportConfig> HealthReportConfigs { get; set; }

    // ==========================================
    // Phase 10 — Economy & Games
    // ==========================================
    public DbSet<RealEstateProperty> RealEstateProperties { get; set; }
    public DbSet<Auction> Auctions { get; set; }
    public DbSet<UserJob> UserJobs { get; set; }
    public DbSet<UserLoan> UserLoans { get; set; }
    public DbSet<LoanHistory> LoanHistories { get; set; }
    public DbSet<EconomySeason> EconomySeasons { get; set; }
    public DbSet<SeasonEarnings> SeasonEarnings { get; set; }
    public DbSet<UserLootBox> UserLootBoxes { get; set; }
    public DbSet<CryptoHolding> CryptoHoldings { get; set; }
    public DbSet<CryptoCoin> CryptoCoins { get; set; }
    public DbSet<UserBusiness> UserBusinesses { get; set; }
    public DbSet<BusinessEmployee> BusinessEmployees { get; set; }
    public DbSet<TaxGovernment> TaxGovernments { get; set; }
    public DbSet<ElectionVote> ElectionVotes { get; set; }
    public DbSet<TriviaTournamentEntry> TriviaTournamentEntries { get; set; }
    public DbSet<UserPokemon> UserPokemon { get; set; }
    public DbSet<DungeonPlayer> DungeonPlayers { get; set; }
    public DbSet<DungeonItem> DungeonItems { get; set; }
    public DbSet<RaidBoss> RaidBosses { get; set; }
    public DbSet<RaidBossParticipant> RaidBossParticipants { get; set; }
    public DbSet<RaidBossConfig> RaidBossConfigs { get; set; }
    public DbSet<RaceCar> RaceCars { get; set; }
    public DbSet<CollectibleCard> CollectibleCards { get; set; }
    public DbSet<PuzzleScore> PuzzleScores { get; set; }
    public DbSet<StoryProgress> StoryProgress { get; set; }
    public DbSet<IdlePlayer> IdlePlayers { get; set; }
    public DbSet<ChessGameModel> ChessGames { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<UserTitle> UserTitles { get; set; }
    public DbSet<BattlePassProgress> BattlePassProgress { get; set; }
    public DbSet<BattlePassConfig> BattlePassConfigs { get; set; }
    public DbSet<DailyChallenge> DailyChallenges { get; set; }
    public DbSet<LoreEntry> LoreEntries { get; set; }
    public DbSet<PlayerDiscovery> PlayerDiscoveries { get; set; }
    public DbSet<TreasureMap> TreasureMaps { get; set; }
    public DbSet<WorldEvent> WorldEvents { get; set; }
    public DbSet<GatheringProfile> GatheringProfiles { get; set; }
    public DbSet<CraftingProfile> CraftingProfiles { get; set; }
    public DbSet<PlayerInventoryItem> PlayerInventoryItems { get; set; }
    public DbSet<PlayerHouse> PlayerHouses { get; set; }
    public DbSet<HouseRoom> HouseRooms { get; set; }
    public DbSet<HouseFurniture> HouseFurniture { get; set; }
    public DbSet<GuestBookEntry> GuestBookEntries { get; set; }

    // ==========================================
    // Phase 11 — Social, Profiles & XP
    // ==========================================
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<ProfileBackground> ProfileBackgrounds { get; set; }
    public DbSet<OwnedBackground> OwnedBackgrounds { get; set; }
    public DbSet<Marriage> Marriages { get; set; }
    public DbSet<Adoption> Adoptions { get; set; }
    public DbSet<UserKarma> UserKarmas { get; set; }
    public DbSet<KarmaVote> KarmaVotes { get; set; }
    public DbSet<UserSocial> UserSocials { get; set; }
    public DbSet<ActivityHeatmap> ActivityHeatmaps { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<UserMood> UserMoods { get; set; }
    public DbSet<TimeCapsule> TimeCapsules { get; set; }
    public DbSet<IntroConfig> IntroConfigs { get; set; }
    public DbSet<SocialStat> SocialStats { get; set; }
    public DbSet<VoiceStat> VoiceStats { get; set; }
    public DbSet<VoicePartner> VoicePartners { get; set; }
    public DbSet<UserPrestige> UserPrestiges { get; set; }
    public DbSet<XpBooster> XpBoosters { get; set; }
    public DbSet<SeasonConfig> SeasonConfigs { get; set; }
    public DbSet<SeasonProgress> SeasonProgresses { get; set; }
    public DbSet<VoiceXpConfig> VoiceXpConfigs { get; set; }
    public DbSet<LevelUpMessage> LevelUpMessages { get; set; }
    public DbSet<XpSnapshot> XpSnapshots { get; set; }
    public DbSet<LevelColorConfig> LevelColorConfigs { get; set; }
    public DbSet<XpChallenge> XpChallenges { get; set; }
    public DbSet<XpChallengeProgress> XpChallengeProgresses { get; set; }
    public DbSet<XpTeam> XpTeams { get; set; }
    public DbSet<XpTeamMember> XpTeamMembers { get; set; }
    public DbSet<XpDecayConfig> XpDecayConfigs { get; set; }

    // ==========================================
    // Phase 12 — Feeds, Dashboard & Developer
    // ==========================================
    public DbSet<RedditFollow> RedditFollows { get; set; }
    public DbSet<XFeedFollow> XFeedFollows { get; set; }
    public DbSet<TwitchClipFollow> TwitchClipFollows { get; set; }
    public DbSet<SteamSaleWatch> SteamSaleWatches { get; set; }
    public DbSet<AnimeTrack> AnimeTracks { get; set; }
    public DbSet<SportsFollow> SportsFollows { get; set; }
    public DbSet<CryptoAlert> CryptoAlerts { get; set; }
    public DbSet<RssFeedEntry> RssFeedEntries { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<AutoFlow> AutoFlows { get; set; }
    public DbSet<ScheduledTask> ScheduledTasks { get; set; }
    public DbSet<EmbedTemplate> EmbedTemplates { get; set; }
    public DbSet<DashWebhook> DashWebhooks { get; set; }
    public DbSet<WhiteLabelConfig> WhiteLabelConfigs { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<CustomScript> CustomScripts { get; set; }
    public DbSet<InstalledPlugin> InstalledPlugins { get; set; }

    // Events & Scheduling
    public DbSet<ServerEvent> ServerEvents { get; set; }
    public DbSet<EventRsvp> EventRsvps { get; set; }
    public DbSet<EventReminder> EventReminders { get; set; }
    public DbSet<MovieNightPoll> MovieNightPolls { get; set; }
    public DbSet<MovieNightOption> MovieNightOptions { get; set; }

    // ==========================================
    // Gamification, Customization, Events, PvP
    // ==========================================
    public DbSet<BotCustomization> BotCustomizations { get; set; }
    public DbSet<CustomEmbed> CustomEmbeds { get; set; }
    public DbSet<CustomCommand> CustomCommands { get; set; }

    // Voice, Streaming, Feeds, Predictions
    public DbSet<SoundboardSound> SoundboardSounds { get; set; }
    public DbSet<TempVoiceConfig> TempVoiceConfigs { get; set; }
    public DbSet<TempVoiceChannel> TempVoiceChannels { get; set; }
    public DbSet<VoiceSessionLog> VoiceSessionLogs { get; set; }
    public DbSet<StreamAlert> StreamAlerts { get; set; }
    public DbSet<ContentSchedule> ContentSchedules { get; set; }
    public DbSet<ChannelPointsConfig> ChannelPointsConfigs { get; set; }
    public DbSet<UserChannelPoints> UserChannelPoints { get; set; }
    public DbSet<ChannelPointReward> ChannelPointRewards { get; set; }
    public DbSet<Prediction> Predictions { get; set; }
    public DbSet<PredictionBet> PredictionBets { get; set; }
    public DbSet<FanArtSubmission> FanArtSubmissions { get; set; }
    public DbSet<FeedSubscription> FeedSubscriptions { get; set; }
    public DbSet<UptimeMonitor> UptimeMonitors { get; set; }

    // Developer, XP, Feature Flags
    public DbSet<BotPlugin> BotPlugins { get; set; }
    public DbSet<WebhookEndpoint> WebhookEndpoints { get; set; }
    public DbSet<FeatureFlag> FeatureFlags { get; set; }
    public DbSet<CommandLog> CommandLogs { get; set; }
    public DbSet<XpMultiplier> XpMultipliers { get; set; }
    public DbSet<XpChallengeEntry> XpChallengeEntries { get; set; }
    public DbSet<XpChallengeParticipant> XpChallengeParticipants { get; set; }
    public DbSet<SantiScheduledPost> SantiScheduledPosts { get; set; }
    public DbSet<NsfwRpConfig> NsfwRpConfigs { get; set; }
    public DbSet<EmbedFixConfig> EmbedFixConfigs { get; set; }

    // ==========================================
    // Missing Model Registrations (wiring fix)
    // ==========================================
    public DbSet<SkillTree> SkillTrees { get; set; }
    public DbSet<PrestigeData> PrestigeData { get; set; }
    public DbSet<DungeonModifier> DungeonModifiers { get; set; }
    public DbSet<Bounty> Bounties { get; set; }
    public DbSet<TreasureHunt> TreasureHunts { get; set; }
    public DbSet<MarriageExpansion> MarriageExpansions { get; set; }
    public DbSet<Horoscope> Horoscopes { get; set; }
    public DbSet<GoalTracker> GoalTrackers { get; set; }
    public DbSet<ServerNewspaper> ServerNewspapers { get; set; }
    public DbSet<PvpStats> PvpStats { get; set; }
    public DbSet<TournamentModel> Tournaments { get; set; }
    public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
    public DbSet<QuestProgress> QuestProgress { get; set; }
    public DbSet<QuestLog> QuestLogs { get; set; }
    public DbSet<FactionStanding> FactionStandings { get; set; }
    public DbSet<PremiumGuild> PremiumGuilds { get; set; }


    #region Mandatory Provider-Specific Values

    protected abstract string CurrencyTransactionOtherIdDefaultValue { get; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // load all entities from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SantiContext).Assembly);

        #region Notify

        modelBuilder.Entity<Notify>(e =>
        {
            e.HasAlternateKey(x => new
            {
                x.GuildId,
                Event = x.Type
            });
        });

        #endregion

        #region TempRoles

        modelBuilder.Entity<TempRole>(e =>
        {
            e.HasAlternateKey(x => new
            {
                x.GuildId,
                x.UserId,
                x.RoleId
            });

            e.HasIndex(x => x.ExpiresAt);
        });

        #endregion

        #region GuildColors

        modelBuilder.Entity<GuildColors>()
            .HasIndex(x => x.GuildId)
            .IsUnique(true);

        #endregion

        #region Button Roles

        modelBuilder.Entity<ButtonRole>(br =>
        {
            br.HasIndex(x => x.GuildId)
                .IsUnique(false);

            br.HasAlternateKey(x => new
            {
                x.RoleId,
                x.MessageId,
            });
        });

        #endregion

        #region New Sar

        modelBuilder.Entity<SarGroup>(sg =>
        {
            sg.HasAlternateKey(x => new
            {
                x.GuildId,
                x.GroupNumber
            });

            sg.HasMany(x => x.Roles)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sar>()
            .HasAlternateKey(x => new
            {
                x.GuildId,
                x.RoleId
            });

        modelBuilder.Entity<SarAutoDelete>()
            .HasIndex(x => x.GuildId)
            .IsUnique();

        #endregion

        #region Rakeback

        modelBuilder.Entity<Rakeback>()
            .HasKey(x => x.UserId);

        #endregion

        #region UserBetStats

        modelBuilder.Entity<UserBetStats>(ubs =>
        {
            ubs.HasIndex(x => new
                {
                    x.UserId,
                    x.Game
                })
                .IsUnique();

            ubs.HasIndex(x => x.MaxWin)
                .IsUnique(false);
        });

        #endregion

        #region Flag Translate

        modelBuilder.Entity<FlagTranslateChannel>()
            .HasIndex(x => new
            {
                x.GuildId,
                x.ChannelId
            })
            .IsUnique();

        #endregion

        #region NCanvas

        modelBuilder.Entity<NCPixel>()
            .HasAlternateKey(x => x.Position);

        modelBuilder.Entity<NCPixel>()
            .HasIndex(x => x.OwnerId);

        #endregion

        #region QUOTES

        var quoteEntity = modelBuilder.Entity<Quote>();
        quoteEntity.HasIndex(x => x.GuildId);
        quoteEntity.HasIndex(x => x.Keyword);

        #endregion

        #region GuildConfig

        var configEntity = modelBuilder.Entity<GuildConfig>();

        configEntity.HasIndex(c => c.GuildId)
            .IsUnique();

        configEntity.Property(x => x.VerboseErrors)
            .HasDefaultValue(true);

        // end shop

        modelBuilder.Entity<PlantedCurrency>().HasIndex(x => x.MessageId).IsUnique();

        modelBuilder.Entity<PlantedCurrency>().HasIndex(x => x.ChannelId);

        configEntity.HasIndex(x => x.WarnExpireHours).IsUnique(false);

        #endregion

        modelBuilder.Entity<WarningPunishment>(b =>
        {
            b.HasAlternateKey(x => new
            {
                x.GuildId,
                x.Count
            });
        });

        #region MusicPlaylists

        var musicPlaylistEntity = modelBuilder.Entity<MusicPlaylist>();

        musicPlaylistEntity.HasMany(p => p.Songs).WithOne().OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Waifus

        // WaifuInfo, WaifuFan, WaifuPeriod configured via IEntityTypeConfiguration

        #endregion

        #region DiscordUser

        modelBuilder.Entity<DiscordUser>(du =>
        {
            du.Property(x => x.IsClubAdmin)
                .HasDefaultValue(false);

            du.Property(x => x.TotalXp)
                .HasDefaultValue(0);

            du.Property(x => x.CurrencyAmount)
                .HasDefaultValue(0);

            du.HasAlternateKey(w => w.UserId);
            du.HasOne(x => x.Club)
                .WithMany(x => x.Members)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            du.HasIndex(x => x.TotalXp);
            du.HasIndex(x => x.CurrencyAmount);
            du.HasIndex(x => x.UserId);
            du.HasIndex(x => x.Username);
        });

        #endregion

        #region Warnings

        modelBuilder.Entity<Warning>(warn =>
        {
            warn.HasIndex(x => x.GuildId);
            warn.HasIndex(x => x.UserId);
            warn.HasIndex(x => x.DateAdded);
            warn.Property(x => x.Weight).HasDefaultValue(1);
        });

        #endregion

        #region XpStats

        var xps = modelBuilder.Entity<UserXpStats>();
        xps.HasIndex(x => new
            {
                x.UserId,
                x.GuildId
            })
            .IsUnique();

        xps.HasIndex(x => x.UserId);
        xps.HasIndex(x => x.GuildId);
        xps.HasIndex(x => x.Xp);

        #endregion

        #region Club

        var ci = modelBuilder.Entity<ClubInfo>();
        ci.HasOne(x => x.Owner)
            .WithOne()
            .HasForeignKey<ClubInfo>(x => x.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        ci.HasIndex(x => new
            {
                x.Name
            })
            .IsUnique();

        #endregion

        #region ClubManytoMany

        modelBuilder.Entity<ClubApplicants>()
            .HasKey(t => new
            {
                t.ClubId,
                t.UserId
            });

        modelBuilder.Entity<ClubApplicants>()
            .HasOne(pt => pt.User)
            .WithMany();

        modelBuilder.Entity<ClubApplicants>()
            .HasOne(pt => pt.Club)
            .WithMany(x => x.Applicants);

        modelBuilder.Entity<ClubBans>()
            .HasKey(t => new
            {
                t.ClubId,
                t.UserId
            });

        modelBuilder.Entity<ClubBans>()
            .HasOne(pt => pt.User)
            .WithMany();

        modelBuilder.Entity<ClubBans>()
            .HasOne(pt => pt.Club)
            .WithMany(x => x.Bans);

        #endregion

        #region CurrencyTransactions

        modelBuilder.Entity<CurrencyTransaction>(e =>
        {
            e.HasIndex(x => x.UserId)
                .IsUnique(false);

            e.Property(x => x.OtherId)
                .HasDefaultValueSql(CurrencyTransactionOtherIdDefaultValue);

            e.Property(x => x.Type)
                .IsRequired();

            e.Property(x => x.Extra)
                .IsRequired();
        });

        #endregion

        #region Reminders

        modelBuilder.Entity<Reminder>().HasIndex(x => x.When);

        #endregion

        #region BanTemplate

        modelBuilder.Entity<BanTemplate>().HasIndex(x => x.GuildId).IsUnique();
        modelBuilder.Entity<BanTemplate>()
            .Property(x => x.PruneDays)
            .HasDefaultValue(null)
            .IsRequired(false);

        #endregion

        #region Perm Override

        modelBuilder.Entity<DiscordPermOverride>()
            .HasIndex(x => new
            {
                x.GuildId,
                x.Command
            })
            .IsUnique();

        #endregion

        #region Music

        modelBuilder.Entity<MusicPlayerSettings>().HasIndex(x => x.GuildId).IsUnique();

        modelBuilder.Entity<MusicPlayerSettings>().Property(x => x.Volume).HasDefaultValue(100);

        #endregion

        #region Reaction roles

        modelBuilder.Entity<ReactionRoleV2>(rr2 =>
        {
            rr2.HasIndex(x => x.GuildId)
                .IsUnique(false);

            rr2.HasIndex(x => new
                {
                    x.MessageId,
                    x.Emote
                })
                .IsUnique();
        });

        #endregion

        #region LogSettings

        modelBuilder.Entity<LogSetting>(ls => ls.HasIndex(x => x.GuildId).IsUnique());

        modelBuilder.Entity<LogSetting>(ls => ls
            .HasMany(x => x.LogIgnores)
            .WithOne(x => x.LogSetting)
            .OnDelete(DeleteBehavior.Cascade));

        modelBuilder.Entity<IgnoredLogItem>(ili => ili
            .HasIndex(x => new
            {
                x.LogSettingId,
                x.LogItemId,
                x.ItemType
            })
            .IsUnique());

        #endregion

        modelBuilder.Entity<ImageOnlyChannel>(ioc => ioc.HasIndex(x => x.ChannelId).IsUnique());

        var atch = modelBuilder.Entity<AutoTranslateChannel>();
        atch.HasIndex(x => x.GuildId).IsUnique(false);

        atch.HasIndex(x => x.ChannelId).IsUnique();

        atch.HasMany(x => x.Users).WithOne(x => x.Channel).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AutoTranslateUser>(atu => atu.HasAlternateKey(x => new
        {
            x.ChannelId,
            x.UserId
        }));

        #region BANK

        modelBuilder.Entity<BankUser>(bu => bu.HasIndex(x => x.UserId).IsUnique());

        #endregion


        #region Patron

        // currency rewards
        var pr = modelBuilder.Entity<RewardedUser>();
        pr.HasIndex(x => x.PlatformUserId).IsUnique();

        // patrons
        // patrons are not identified by their user id, but by their platform user id
        // as multiple accounts (even maybe on different platforms) could have
        // the same account connected to them
        modelBuilder.Entity<PatronUser>(pu =>
        {
            pu.HasIndex(x => x.UniquePlatformUserId).IsUnique();
            pu.HasKey(x => x.UserId);
        });

        // quotes are per user id

        #endregion

        #region Xp Item Shop

        modelBuilder.Entity<XpShopOwnedItem>(
            x =>
            {
                // user can own only one of each item
                x.HasIndex(model => new
                    {
                        model.UserId,
                        model.ItemType,
                        model.ItemKey
                    })
                    .IsUnique();
            });

        #endregion

        #region AutoPublish

        modelBuilder.Entity<AutoPublishChannel>(apc => apc
            .HasIndex(x => x.GuildId)
            .IsUnique());

        #endregion

        #region GamblingStats

        modelBuilder.Entity<GamblingStats>(gs => gs
            .HasIndex(x => x.Feature)
            .IsUnique());

        #endregion

        #region Sticky Roles

        modelBuilder.Entity<StickyRole>(sr => sr.HasIndex(x => new
            {
                x.GuildId,
                x.UserId
            })
            .IsUnique());

        #endregion


        #region Giveaway

        modelBuilder.Entity<GiveawayModel>()
            .HasMany(x => x.Participants)
            .WithOne()
            .HasForeignKey(x => x.GiveawayId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GiveawayUser>(gu => gu
            .HasIndex(x => new
            {
                x.GiveawayId,
                x.UserId
            })
            .IsUnique());

        #endregion

        #region Todo

        modelBuilder.Entity<TodoModel>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<TodoModel>()
            .HasIndex(x => x.UserId)
            .IsUnique(false);

        modelBuilder.Entity<ArchivedTodoListModel>()
            .HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.ArchiveId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region GreetSettings

        modelBuilder
            .Entity<GreetSettings>(gs => gs.HasIndex(x => new
                {
                    x.GuildId,
                    x.GreetType
                })
                .IsUnique());

        modelBuilder.Entity<GreetSettings>(gs =>
        {
            gs
                .Property(x => x.IsEnabled)
                .HasDefaultValue(false);

            gs
                .Property(x => x.AutoDeleteTimer)
                .HasDefaultValue(0);
        });

        #endregion
    }

#if DEBUG
    private static readonly ILoggerFactory _debugLoggerFactory = LoggerFactory.Create(x => x.AddConsole());
#endif

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        optionsBuilder.UseLoggerFactory(_debugLoggerFactory);
#endif

        optionsBuilder.ConfigureWarnings(x => x.Log(RelationalEventId.PendingModelChangesWarning)
            .Ignore());
    }
}