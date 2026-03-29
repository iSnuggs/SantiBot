BEGIN TRANSACTION;

-- ActivityHeatmaps
CREATE TABLE IF NOT EXISTS "ActivityHeatmaps" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Date" TEXT NOT NULL,
    "MessageCount" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ActivityHeatmaps_GuildId" ON "ActivityHeatmaps" ("GuildId");

-- Adoptions
CREATE TABLE IF NOT EXISTS "Adoptions" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ParentId" INTEGER NOT NULL,
    "ChildId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Adoptions_GuildId" ON "Adoptions" ("GuildId");

-- AnimeTracks
CREATE TABLE IF NOT EXISTS "AnimeTracks" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "AniListId" INTEGER NOT NULL,
    "Title" TEXT NULL,
    "LastEpisode" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_AnimeTracks_GuildId" ON "AnimeTracks" ("GuildId");

-- ApiKeys
CREATE TABLE IF NOT EXISTS "ApiKeys" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Key" TEXT NULL,
    "IsRevoked" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ApiKeys_GuildId" ON "ApiKeys" ("GuildId");

-- Auctions
CREATE TABLE IF NOT EXISTS "Auctions" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "SellerId" INTEGER NOT NULL,
    "SellerName" TEXT NULL,
    "ItemDescription" TEXT NULL,
    "StartPrice" INTEGER NOT NULL,
    "CurrentBid" INTEGER NOT NULL,
    "HighestBidderId" INTEGER NOT NULL,
    "HighestBidderName" TEXT NULL,
    "EndsAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Auctions_GuildId" ON "Auctions" ("GuildId");

-- AutoArchiveConfigs
CREATE TABLE IF NOT EXISTS "AutoArchiveConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "InactiveDays" INTEGER NOT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS "IX_AutoArchiveConfigs_GuildId" ON "AutoArchiveConfigs" ("GuildId");

-- AutoArchiveExclusions
CREATE TABLE IF NOT EXISTS "AutoArchiveExclusions" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_AutoArchiveExclusions_GuildId" ON "AutoArchiveExclusions" ("GuildId");

-- BanSyncConfigs
CREATE TABLE IF NOT EXISTS "BanSyncConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "LinkedGuildId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_BanSyncConfigs_GuildId" ON "BanSyncConfigs" ("GuildId");

-- BanSyncEntries
CREATE TABLE IF NOT EXISTS "BanSyncEntries" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "BannedUserId" INTEGER NOT NULL,
    "Reason" TEXT NULL,
    "BannedByUserId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_BanSyncEntries_GuildId" ON "BanSyncEntries" ("GuildId");

-- BlueskyFeedSubs
CREATE TABLE IF NOT EXISTS "BlueskyFeedSubs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "BlueskyHandle" TEXT NULL,
    "LastPostUri" TEXT NULL,
    "LastChecked" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_BlueskyFeedSubs_GuildId" ON "BlueskyFeedSubs" ("GuildId");

-- BusinessEmployees
CREATE TABLE IF NOT EXISTS "BusinessEmployees" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "BusinessId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "HiredAt" TEXT NOT NULL,
    "LastWorked" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_BusinessEmployees_GuildId" ON "BusinessEmployees" ("GuildId");

-- CalendarEvents
CREATE TABLE IF NOT EXISTS "CalendarEvents" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "CreatorId" INTEGER NOT NULL,
    "EventDate" TEXT NOT NULL,
    "Title" TEXT NULL,
    "Description" TEXT NULL,
    "ReminderMinutesBefore" INTEGER NULL,
    "ReminderSent" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_CalendarEvents_GuildId" ON "CalendarEvents" ("GuildId");

-- ChannelActivities
CREATE TABLE IF NOT EXISTS "ChannelActivities" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "MessageCount" INTEGER NOT NULL,
    "TrackedDate" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ChannelActivities_GuildId" ON "ChannelActivities" ("GuildId");

-- ChannelTemplates
CREATE TABLE IF NOT EXISTS "ChannelTemplates" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "SettingsJson" TEXT NULL,
    "CreatedByUserId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ChannelTemplates_GuildId" ON "ChannelTemplates" ("GuildId");

-- ChessGames
CREATE TABLE IF NOT EXISTS "ChessGames" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "WhitePlayerId" INTEGER NOT NULL,
    "BlackPlayerId" INTEGER NOT NULL,
    "WhiteWins" INTEGER NOT NULL,
    "BlackWins" INTEGER NOT NULL,
    "Draws" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ChessGames_GuildId" ON "ChessGames" ("GuildId");

-- CollectibleCards
CREATE TABLE IF NOT EXISTS "CollectibleCards" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "CardName" TEXT NULL,
    "Rarity" TEXT NULL,
    "Set" TEXT NULL,
    "Quantity" INTEGER NOT NULL
);

-- ContentAgeGates
CREATE TABLE IF NOT EXISTS "ContentAgeGates" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "RequiredRoleId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ContentAgeGates_GuildId" ON "ContentAgeGates" ("GuildId");

-- CryptoAlerts
CREATE TABLE IF NOT EXISTS "CryptoAlerts" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "CoinId" TEXT NULL,
    "Direction" TEXT NULL,
    "TargetPrice" REAL NOT NULL,
    "Triggered" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_CryptoAlerts_GuildId" ON "CryptoAlerts" ("GuildId");

-- CryptoCoins
CREATE TABLE IF NOT EXISTS "CryptoCoins" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "Price" INTEGER NOT NULL,
    "LastUpdated" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_CryptoCoins_GuildId" ON "CryptoCoins" ("GuildId");

-- CryptoHoldings
CREATE TABLE IF NOT EXISTS "CryptoHoldings" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "CoinName" TEXT NULL,
    "Amount" REAL NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_CryptoHoldings_GuildId" ON "CryptoHoldings" ("GuildId");

-- DashWebhooks
CREATE TABLE IF NOT EXISTS "DashWebhooks" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Url" TEXT NULL,
    "Event" TEXT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS "IX_DashWebhooks_GuildId" ON "DashWebhooks" ("GuildId");

-- DropdownRoleOptions
CREATE TABLE IF NOT EXISTS "DropdownRoleOptions" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "PanelId" INTEGER NOT NULL,
    "GuildId" INTEGER NOT NULL,
    "MessageId" INTEGER NOT NULL,
    "Label" TEXT NULL,
    "Description" TEXT NULL,
    "RoleId" INTEGER NOT NULL,
    "Emote" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_DropdownRoleOptions_GuildId" ON "DropdownRoleOptions" ("GuildId");

-- DropdownRolePanels
CREATE TABLE IF NOT EXISTS "DropdownRolePanels" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "MessageId" INTEGER NOT NULL,
    "Title" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_DropdownRolePanels_GuildId" ON "DropdownRolePanels" ("GuildId");

-- DungeonPlayers
CREATE TABLE IF NOT EXISTS "DungeonPlayers" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "DungeonsCleared" INTEGER NOT NULL,
    "MonstersKilled" INTEGER NOT NULL,
    "TotalLoot" INTEGER NOT NULL
);

-- EconomySeasons
CREATE TABLE IF NOT EXISTS "EconomySeasons" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "SeasonNumber" INTEGER NOT NULL,
    "StartedAt" TEXT NOT NULL,
    "EndedAt" TEXT NULL,
    "IsActive" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_EconomySeasons_GuildId" ON "EconomySeasons" ("GuildId");

-- ElectionVotes
CREATE TABLE IF NOT EXISTS "ElectionVotes" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "VoterId" INTEGER NOT NULL,
    "CandidateId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ElectionVotes_GuildId" ON "ElectionVotes" ("GuildId");

-- EmbedTemplates
CREATE TABLE IF NOT EXISTS "EmbedTemplates" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "Category" TEXT NULL,
    "EmbedJson" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_EmbedTemplates_GuildId" ON "EmbedTemplates" ("GuildId");

-- EvidenceItems
CREATE TABLE IF NOT EXISTS "EvidenceItems" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "CaseId" INTEGER NOT NULL,
    "Url" TEXT NULL,
    "Note" TEXT NULL,
    "AddedByUserId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_EvidenceItems_GuildId" ON "EvidenceItems" ("GuildId");

-- Friendships
CREATE TABLE IF NOT EXISTS "Friendships" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "User1Id" INTEGER NOT NULL,
    "User2Id" INTEGER NOT NULL,
    "Accepted" INTEGER NOT NULL,
    "InteractionCount" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Friendships_GuildId" ON "Friendships" ("GuildId");

-- HealthReportConfigs
CREATE TABLE IF NOT EXISTS "HealthReportConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "AutoEnabled" INTEGER NOT NULL,
    "LastReportDate" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_HealthReportConfigs_GuildId" ON "HealthReportConfigs" ("GuildId");

-- IdlePlayers
CREATE TABLE IF NOT EXISTS "IdlePlayers" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "Resources" INTEGER NOT NULL,
    "ResourcesPerSecond" REAL NOT NULL,
    "ClickPower" INTEGER NOT NULL,
    "PrestigeLevel" INTEGER NOT NULL,
    "PrestigeMultiplier" REAL NOT NULL,
    "LastCollected" TEXT NOT NULL,
    "Upgrades" TEXT NULL
);

-- InstalledPlugins
CREATE TABLE IF NOT EXISTS "InstalledPlugins" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "PluginName" TEXT NULL,
    "Version" TEXT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS "IX_InstalledPlugins_GuildId" ON "InstalledPlugins" ("GuildId");

-- IntroConfigs
CREATE TABLE IF NOT EXISTS "IntroConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Template" TEXT NULL,
    "Enabled" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_IntroConfigs_GuildId" ON "IntroConfigs" ("GuildId");

-- InviteWhitelistConfigs
CREATE TABLE IF NOT EXISTS "InviteWhitelistConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "IsEnabled" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_InviteWhitelistConfigs_GuildId" ON "InviteWhitelistConfigs" ("GuildId");

-- InviteWhitelists
CREATE TABLE IF NOT EXISTS "InviteWhitelists" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "AllowedServerId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_InviteWhitelists_GuildId" ON "InviteWhitelists" ("GuildId");

-- KarmaVotes
CREATE TABLE IF NOT EXISTS "KarmaVotes" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "VoterId" INTEGER NOT NULL,
    "TargetUserId" INTEGER NOT NULL,
    "MessageId" INTEGER NOT NULL,
    "IsUpvote" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_KarmaVotes_GuildId" ON "KarmaVotes" ("GuildId");

-- KickStreamFollows
CREATE TABLE IF NOT EXISTS "KickStreamFollows" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "NotifyChannelId" INTEGER NOT NULL,
    "KickUsername" TEXT NULL,
    "IsLive" INTEGER NOT NULL,
    "CustomMessage" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_KickStreamFollows_GuildId" ON "KickStreamFollows" ("GuildId");

-- LevelColorConfigs
CREATE TABLE IF NOT EXISTS "LevelColorConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Enabled" INTEGER NOT NULL,
    "StartColor" TEXT NULL,
    "EndColor" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_LevelColorConfigs_GuildId" ON "LevelColorConfigs" ("GuildId");

-- LevelUpMessages
CREATE TABLE IF NOT EXISTS "LevelUpMessages" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "MessageTemplate" TEXT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Enabled" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_LevelUpMessages_GuildId" ON "LevelUpMessages" ("GuildId");

-- LoanHistories
CREATE TABLE IF NOT EXISTS "LoanHistories" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Amount" INTEGER NOT NULL,
    "RepaidOnTime" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_LoanHistories_GuildId" ON "LoanHistories" ("GuildId");

-- LockdownPresets
CREATE TABLE IF NOT EXISTS "LockdownPresets" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "PermissionsJson" TEXT NULL,
    "CreatedByUserId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_LockdownPresets_GuildId" ON "LockdownPresets" ("GuildId");

-- Marriages
CREATE TABLE IF NOT EXISTS "Marriages" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "User1Id" INTEGER NOT NULL,
    "User2Id" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Marriages_GuildId" ON "Marriages" ("GuildId");

-- ModActionTemplates
CREATE TABLE IF NOT EXISTS "ModActionTemplates" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "Reason" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ModActionTemplates_GuildId" ON "ModActionTemplates" ("GuildId");

-- ModShifts
CREATE TABLE IF NOT EXISTS "ModShifts" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "DayOfWeek" INTEGER NOT NULL,
    "StartHour" INTEGER NOT NULL,
    "EndHour" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ModShifts_GuildId" ON "ModShifts" ("GuildId");

-- ModTranslateConfigs
CREATE TABLE IF NOT EXISTS "ModTranslateConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "TargetLanguage" TEXT NULL,
    "IsEnabled" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ModTranslateConfigs_GuildId" ON "ModTranslateConfigs" ("GuildId");

-- OwnedBackgrounds
CREATE TABLE IF NOT EXISTS "OwnedBackgrounds" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "BackgroundId" TEXT NULL
);

-- ProfileBackgrounds
CREATE TABLE IF NOT EXISTS "ProfileBackgrounds" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "BackgroundId" TEXT NULL,
    "Name" TEXT NULL,
    "HexColor" TEXT NULL,
    "Price" INTEGER NOT NULL
);

-- PuzzleScores
CREATE TABLE IF NOT EXISTS "PuzzleScores" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "TotalSolved" INTEGER NOT NULL,
    "TotalPoints" INTEGER NOT NULL,
    "LastSolvedDate" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_PuzzleScores_GuildId" ON "PuzzleScores" ("GuildId");

-- RaceCars
CREATE TABLE IF NOT EXISTS "RaceCars" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "Speed" INTEGER NOT NULL,
    "Handling" INTEGER NOT NULL,
    "Nitro" INTEGER NOT NULL,
    "Wins" INTEGER NOT NULL,
    "Races" INTEGER NOT NULL
);

-- RealEstateProperties
CREATE TABLE IF NOT EXISTS "RealEstateProperties" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "PropertyType" TEXT NULL,
    "UpgradeLevel" INTEGER NOT NULL,
    "LastCollected" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_RealEstateProperties_GuildId" ON "RealEstateProperties" ("GuildId");

-- RedditFollows
CREATE TABLE IF NOT EXISTS "RedditFollows" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Subreddit" TEXT NULL,
    "LastPostId" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_RedditFollows_GuildId" ON "RedditFollows" ("GuildId");

-- RegexAutomodRules
CREATE TABLE IF NOT EXISTS "RegexAutomodRules" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Pattern" TEXT NULL,
    "Action" INTEGER NOT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1,
    "AddedByUserId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_RegexAutomodRules_GuildId" ON "RegexAutomodRules" ("GuildId");

-- RssFeedEntries
CREATE TABLE IF NOT EXISTS "RssFeedEntries" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Url" TEXT NULL,
    "LastItemId" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_RssFeedEntries_GuildId" ON "RssFeedEntries" ("GuildId");

-- ScheduledTasks
CREATE TABLE IF NOT EXISTS "ScheduledTasks" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Message" TEXT NULL,
    "CronExpression" TEXT NULL,
    "NextRun" TEXT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS "IX_ScheduledTasks_GuildId" ON "ScheduledTasks" ("GuildId");

-- SeasonConfigs
CREATE TABLE IF NOT EXISTS "SeasonConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "SeasonNumber" INTEGER NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NOT NULL,
    "Active" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_SeasonConfigs_GuildId" ON "SeasonConfigs" ("GuildId");

-- SeasonEarnings
CREATE TABLE IF NOT EXISTS "SeasonEarnings" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "SeasonNumber" INTEGER NOT NULL,
    "TotalEarned" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_SeasonEarnings_GuildId" ON "SeasonEarnings" ("GuildId");

-- SeasonProgresses
CREATE TABLE IF NOT EXISTS "SeasonProgresses" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "SeasonNumber" INTEGER NOT NULL,
    "SeasonXp" INTEGER NOT NULL,
    "ClaimedLevel" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_SeasonProgresses_GuildId" ON "SeasonProgresses" ("GuildId");

-- ServerBackups
CREATE TABLE IF NOT EXISTS "ServerBackups" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "BackupJson" TEXT NULL,
    "CreatedByUserId" INTEGER NOT NULL,
    "Description" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ServerBackups_GuildId" ON "ServerBackups" ("GuildId");

-- SlowmodeSchedules
CREATE TABLE IF NOT EXISTS "SlowmodeSchedules" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "SlowmodeSeconds" INTEGER NOT NULL,
    "StartTime" TEXT NOT NULL,
    "EndTime" TEXT NOT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS "IX_SlowmodeSchedules_GuildId" ON "SlowmodeSchedules" ("GuildId");

-- SmartSpamConfigs
CREATE TABLE IF NOT EXISTS "SmartSpamConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "IsEnabled" INTEGER NOT NULL,
    "Threshold" INTEGER NOT NULL,
    "Action" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_SmartSpamConfigs_GuildId" ON "SmartSpamConfigs" ("GuildId");

-- SocialStats
CREATE TABLE IF NOT EXISTS "SocialStats" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "TotalMessages" INTEGER NOT NULL,
    "TotalReactions" INTEGER NOT NULL,
    "TotalVoiceMinutes" INTEGER NOT NULL,
    "HelpfulReactions" INTEGER NOT NULL,
    "WeeklyMessages" INTEGER NOT NULL,
    "WeeklyReactions" INTEGER NOT NULL,
    "WeeklyVoiceMinutes" INTEGER NOT NULL,
    "WeekStart" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_SocialStats_GuildId" ON "SocialStats" ("GuildId");

-- SportsFollows
CREATE TABLE IF NOT EXISTS "SportsFollows" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "League" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_SportsFollows_GuildId" ON "SportsFollows" ("GuildId");

-- SteamSaleWatches
CREATE TABLE IF NOT EXISTS "SteamSaleWatches" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "AppId" TEXT NULL,
    "GameName" TEXT NULL,
    "LastOnSale" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_SteamSaleWatches_GuildId" ON "SteamSaleWatches" ("GuildId");

-- StoryProgress
CREATE TABLE IF NOT EXISTS "StoryProgress" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "QuestId" TEXT NULL,
    "Chapter" INTEGER NOT NULL,
    "ChoicePath" TEXT NULL,
    "IsComplete" INTEGER NOT NULL,
    "RewardsEarned" INTEGER NOT NULL
);

-- TaxGovernments
CREATE TABLE IF NOT EXISTS "TaxGovernments" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "TaxRate" INTEGER NOT NULL,
    "Treasury" INTEGER NOT NULL,
    "ElectedOfficialId" INTEGER NOT NULL,
    "ElectionEndsAt" TEXT NOT NULL,
    "ElectionActive" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_TaxGovernments_GuildId" ON "TaxGovernments" ("GuildId");

-- TimeCapsules
CREATE TABLE IF NOT EXISTS "TimeCapsules" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Message" TEXT NULL,
    "DeliverAt" TEXT NOT NULL,
    "Delivered" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_TimeCapsules_GuildId" ON "TimeCapsules" ("GuildId");

-- TriviaTournamentEntries
CREATE TABLE IF NOT EXISTS "TriviaTournamentEntries" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Username" TEXT NULL,
    "Wins" INTEGER NOT NULL,
    "TotalScore" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_TriviaTournamentEntries_GuildId" ON "TriviaTournamentEntries" ("GuildId");

-- TwitchClipFollows
CREATE TABLE IF NOT EXISTS "TwitchClipFollows" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "TwitchChannel" TEXT NULL,
    "LastClipId" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_TwitchClipFollows_GuildId" ON "TwitchClipFollows" ("GuildId");

-- UserBusinesses
CREATE TABLE IF NOT EXISTS "UserBusinesses" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "OwnerId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "BusinessType" TEXT NULL,
    "Revenue" INTEGER NOT NULL,
    "LastCollected" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserBusinesses_GuildId" ON "UserBusinesses" ("GuildId");

-- UserJobs
CREATE TABLE IF NOT EXISTS "UserJobs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "JobName" TEXT NULL,
    "TimesWorked" INTEGER NOT NULL,
    "LastWorked" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserJobs_GuildId" ON "UserJobs" ("GuildId");

-- UserKarmas
CREATE TABLE IF NOT EXISTS "UserKarmas" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Upvotes" INTEGER NOT NULL,
    "Downvotes" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserKarmas_GuildId" ON "UserKarmas" ("GuildId");

-- UserLoans
CREATE TABLE IF NOT EXISTS "UserLoans" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Principal" INTEGER NOT NULL,
    "AmountOwed" INTEGER NOT NULL,
    "CreditScore" INTEGER NOT NULL,
    "TakenAt" TEXT NOT NULL,
    "LastInterestApplied" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserLoans_GuildId" ON "UserLoans" ("GuildId");

-- UserLootBoxes
CREATE TABLE IF NOT EXISTS "UserLootBoxes" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "CommonBoxes" INTEGER NOT NULL,
    "UncommonBoxes" INTEGER NOT NULL,
    "RareBoxes" INTEGER NOT NULL,
    "LegendaryBoxes" INTEGER NOT NULL,
    "MythicBoxes" INTEGER NOT NULL,
    "UnopenedBoxes" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserLootBoxes_GuildId" ON "UserLootBoxes" ("GuildId");

-- UserMoods
CREATE TABLE IF NOT EXISTS "UserMoods" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Emoji" TEXT NULL,
    "Message" TEXT NULL,
    "ExpiresAt" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserMoods_GuildId" ON "UserMoods" ("GuildId");

-- UserNotes
CREATE TABLE IF NOT EXISTS "UserNotes" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "ModeratorId" INTEGER NOT NULL,
    "Note" TEXT NULL,
    "ActionType" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserNotes_GuildId" ON "UserNotes" ("GuildId");

-- UserPokemon
CREATE TABLE IF NOT EXISTS "UserPokemon" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "Type" TEXT NULL,
    "Level" INTEGER NOT NULL,
    "Hp" INTEGER NOT NULL,
    "MaxHp" INTEGER NOT NULL,
    "Attack" INTEGER NOT NULL,
    "Defense" INTEGER NOT NULL,
    "Xp" INTEGER NOT NULL,
    "XpToNext" INTEGER NOT NULL
);

-- UserPrestiges
CREATE TABLE IF NOT EXISTS "UserPrestiges" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "PrestigeLevel" INTEGER NOT NULL,
    "LastPrestigeDate" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserPrestiges_GuildId" ON "UserPrestiges" ("GuildId");

-- UserProfiles
CREATE TABLE IF NOT EXISTS "UserProfiles" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Bio" TEXT NULL,
    "Title" TEXT NULL,
    "Pronouns" TEXT NULL,
    "Timezone" TEXT NULL,
    "MessageCount" INTEGER NOT NULL,
    "BackgroundId" TEXT NULL,
    "BackgroundName" TEXT NULL,
    "BackgroundColor" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_UserProfiles_GuildId" ON "UserProfiles" ("GuildId");

-- UserSocials
CREATE TABLE IF NOT EXISTS "UserSocials" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "Platform" TEXT NULL,
    "Handle" TEXT NULL
);

-- VoicePartners
CREATE TABLE IF NOT EXISTS "VoicePartners" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "User1Id" INTEGER NOT NULL,
    "User2Id" INTEGER NOT NULL,
    "SharedMinutes" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_VoicePartners_GuildId" ON "VoicePartners" ("GuildId");

-- VoiceStats
CREATE TABLE IF NOT EXISTS "VoiceStats" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "TotalMinutes" INTEGER NOT NULL,
    "FavoriteChannelId" INTEGER NOT NULL,
    "FavoriteChannelMinutes" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_VoiceStats_GuildId" ON "VoiceStats" ("GuildId");

-- VoiceXpConfigs
CREATE TABLE IF NOT EXISTS "VoiceXpConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Enabled" INTEGER NOT NULL,
    "XpPerMinute" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_VoiceXpConfigs_GuildId" ON "VoiceXpConfigs" ("GuildId");

-- WarningPointConfigs
CREATE TABLE IF NOT EXISTS "WarningPointConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Threshold" INTEGER NOT NULL,
    "Action" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_WarningPointConfigs_GuildId" ON "WarningPointConfigs" ("GuildId");

-- WarningPoints
CREATE TABLE IF NOT EXISTS "WarningPoints" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "ModeratorId" INTEGER NOT NULL,
    "Reason" TEXT NULL,
    "Points" INTEGER NOT NULL,
    "Severity" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_WarningPoints_GuildId" ON "WarningPoints" ("GuildId");

-- WhiteLabelConfigs
CREATE TABLE IF NOT EXISTS "WhiteLabelConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "BotName" TEXT NULL,
    "AvatarUrl" TEXT NULL,
    "PrimaryColor" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_WhiteLabelConfigs_GuildId" ON "WhiteLabelConfigs" ("GuildId");

-- XFeedFollows
CREATE TABLE IF NOT EXISTS "XFeedFollows" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Handle" TEXT NULL,
    "LastItemId" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_XFeedFollows_GuildId" ON "XFeedFollows" ("GuildId");

-- XpBoosters
CREATE TABLE IF NOT EXISTS "XpBoosters" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Multiplier" REAL NOT NULL,
    "ExpiresAt" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_XpBoosters_GuildId" ON "XpBoosters" ("GuildId");

-- XpChallengeProgresses
CREATE TABLE IF NOT EXISTS "XpChallengeProgresses" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "ChallengeId" INTEGER NOT NULL,
    "CurrentAmount" INTEGER NOT NULL,
    "Completed" INTEGER NOT NULL,
    "Claimed" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_XpChallengeProgresses_GuildId" ON "XpChallengeProgresses" ("GuildId");

-- XpChallenges
CREATE TABLE IF NOT EXISTS "XpChallenges" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChallengeType" TEXT NULL,
    "Description" TEXT NULL,
    "TargetAmount" INTEGER NOT NULL,
    "BonusXp" INTEGER NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_XpChallenges_GuildId" ON "XpChallenges" ("GuildId");

-- XpDecayConfigs
CREATE TABLE IF NOT EXISTS "XpDecayConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Enabled" INTEGER NOT NULL,
    "InactiveDays" INTEGER NOT NULL,
    "XpLostPerDay" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_XpDecayConfigs_GuildId" ON "XpDecayConfigs" ("GuildId");

-- XpSnapshots
CREATE TABLE IF NOT EXISTS "XpSnapshots" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Xp" INTEGER NOT NULL,
    "Rank" INTEGER NOT NULL,
    "SnapshotDate" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_XpSnapshots_GuildId" ON "XpSnapshots" ("GuildId");

-- XpTeamMembers
CREATE TABLE IF NOT EXISTS "XpTeamMembers" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "TeamId" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_XpTeamMembers_GuildId" ON "XpTeamMembers" ("GuildId");

-- XpTeams
CREATE TABLE IF NOT EXISTS "XpTeams" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "OwnerId" INTEGER NOT NULL,
    "TotalXp" INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_XpTeams_GuildId" ON "XpTeams" ("GuildId");

-- YouTubeFeedSubs
CREATE TABLE IF NOT EXISTS "YouTubeFeedSubs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "YouTubeChannelId" TEXT NULL,
    "YouTubeChannelName" TEXT NULL,
    "LastVideoId" TEXT NULL,
    "LastChecked" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_YouTubeFeedSubs_GuildId" ON "YouTubeFeedSubs" ("GuildId");

-- RaidBosses
CREATE TABLE IF NOT EXISTS "RaidBosses" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "Emoji" TEXT NULL,
    "MaxHp" INTEGER NOT NULL DEFAULT 0,
    "CurrentHp" INTEGER NOT NULL DEFAULT 0,
    "Attack" INTEGER NOT NULL DEFAULT 0,
    "Defense" INTEGER NOT NULL DEFAULT 0,
    "CurrentPhase" INTEGER NOT NULL DEFAULT 1,
    "XpReward" INTEGER NOT NULL DEFAULT 0,
    "CurrencyReward" INTEGER NOT NULL DEFAULT 0,
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    "SpawnedAt" TEXT NOT NULL,
    "DefeatedAt" TEXT NULL,
    "WasRandomSpawn" INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS "IX_RaidBosses_GuildId_IsActive" ON "RaidBosses" ("GuildId", "IsActive");

-- RaidBossParticipants
CREATE TABLE IF NOT EXISTS "RaidBossParticipants" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "RaidBossId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "GuildId" INTEGER NOT NULL,
    "DamageDealt" INTEGER NOT NULL DEFAULT 0,
    "HitCount" INTEGER NOT NULL DEFAULT 0,
    "LastAttackAt" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_RaidBossParticipants_RaidBossId" ON "RaidBossParticipants" ("RaidBossId");
CREATE INDEX IF NOT EXISTS "IX_RaidBossParticipants_GuildId_UserId" ON "RaidBossParticipants" ("GuildId", "UserId");

-- RaidBossConfigs
CREATE TABLE IF NOT EXISTS "RaidBossConfigs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "RandomSpawnsEnabled" INTEGER NOT NULL DEFAULT 1,
    "MinDungeonClears" INTEGER NOT NULL DEFAULT 5,
    "MaxDungeonClears" INTEGER NOT NULL DEFAULT 25,
    "DungeonClearsSinceLastRaid" INTEGER NOT NULL DEFAULT 0,
    "NextSpawnThreshold" INTEGER NOT NULL DEFAULT 10,
    "SpawnChannelId" INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RaidBossConfigs_GuildId" ON "RaidBossConfigs" ("GuildId");

-- Pets
CREATE TABLE IF NOT EXISTS "Pets" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL DEFAULT '',
    "Species" TEXT NOT NULL DEFAULT 'Dog',
    "Emoji" TEXT NOT NULL DEFAULT '',
    "Level" INTEGER NOT NULL DEFAULT 1,
    "Xp" INTEGER NOT NULL DEFAULT 0,
    "Happiness" INTEGER NOT NULL DEFAULT 50,
    "Hunger" INTEGER NOT NULL DEFAULT 50,
    "Energy" INTEGER NOT NULL DEFAULT 100,
    "IsShiny" INTEGER NOT NULL DEFAULT 0,
    "Strength" INTEGER NOT NULL DEFAULT 0,
    "Agility" INTEGER NOT NULL DEFAULT 0,
    "Intelligence" INTEGER NOT NULL DEFAULT 0,
    "AdventureCount" INTEGER NOT NULL DEFAULT 0,
    "BattlesWon" INTEGER NOT NULL DEFAULT 0,
    "LastFedAt" TEXT NOT NULL,
    "LastPlayedAt" TEXT NOT NULL,
    "EvolutionStage" INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS "IX_Pets_UserId_GuildId" ON "Pets" ("UserId", "GuildId");



CREATE TABLE IF NOT EXISTS UserBadges (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    BadgeId TEXT, BadgeName TEXT, Emoji TEXT, Category TEXT,
    Rarity TEXT DEFAULT 'Common', IsDisplayed INTEGER NOT NULL DEFAULT 0,
    EarnedAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_UserBadges_UserId_GuildId ON UserBadges (UserId, GuildId);

CREATE TABLE IF NOT EXISTS UserTitles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    TitleId TEXT, TitleName TEXT, Color TEXT,
    IsActive INTEGER NOT NULL DEFAULT 0,
    EarnedAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_UserTitles_UserId_GuildId ON UserTitles (UserId, GuildId);

CREATE TABLE IF NOT EXISTS BattlePassProgress (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    Season INTEGER NOT NULL DEFAULT 1, CurrentTier INTEGER NOT NULL DEFAULT 1,
    SeasonXp INTEGER NOT NULL DEFAULT 0, IsPremium INTEGER NOT NULL DEFAULT 0,
    DailyChallengesCompleted INTEGER NOT NULL DEFAULT 0,
    WeeklyChallengesCompleted INTEGER NOT NULL DEFAULT 0,
    LastDailyChallengeAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS BattlePassConfigs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, CurrentSeason INTEGER NOT NULL DEFAULT 1,
    SeasonName TEXT DEFAULT 'Season 1', MaxTier INTEGER NOT NULL DEFAULT 50,
    XpPerTier INTEGER NOT NULL DEFAULT 1000, IsActive INTEGER NOT NULL DEFAULT 1,
    SeasonStartedAt TEXT NOT NULL DEFAULT (datetime('now')),
    SeasonEndsAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_BattlePassConfigs_GuildId ON BattlePassConfigs (GuildId);

CREATE TABLE IF NOT EXISTS DailyChallenges (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChallengeId TEXT, Description TEXT,
    XpReward INTEGER NOT NULL DEFAULT 0, ActiveDate TEXT NOT NULL, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS GatheringProfiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    MiningLevel INTEGER NOT NULL DEFAULT 1, MiningXp INTEGER NOT NULL DEFAULT 0,
    WoodcuttingLevel INTEGER NOT NULL DEFAULT 1, WoodcuttingXp INTEGER NOT NULL DEFAULT 0,
    FarmingLevel INTEGER NOT NULL DEFAULT 1, FarmingXp INTEGER NOT NULL DEFAULT 0,
    FishingSkillLevel INTEGER NOT NULL DEFAULT 1, FishingSkillXp INTEGER NOT NULL DEFAULT 0,
    HerbGatheringLevel INTEGER NOT NULL DEFAULT 1, HerbGatheringXp INTEGER NOT NULL DEFAULT 0,
    LastMinedAt TEXT, LastChoppedAt TEXT, LastHarvestedAt TEXT, LastGatheredAt TEXT, DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_GatheringProfiles_UserId_GuildId ON GatheringProfiles (UserId, GuildId);

CREATE TABLE IF NOT EXISTS CraftingProfiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    CookingLevel INTEGER NOT NULL DEFAULT 1, CookingXp INTEGER NOT NULL DEFAULT 0,
    AlchemyLevel INTEGER NOT NULL DEFAULT 1, AlchemyXp INTEGER NOT NULL DEFAULT 0,
    BlacksmithingLevel INTEGER NOT NULL DEFAULT 1, BlacksmithingXp INTEGER NOT NULL DEFAULT 0,
    EnchantingLevel INTEGER NOT NULL DEFAULT 1, EnchantingXp INTEGER NOT NULL DEFAULT 0,
    JewelcraftingLevel INTEGER NOT NULL DEFAULT 1, JewelcraftingXp INTEGER NOT NULL DEFAULT 0,
    DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_CraftingProfiles_UserId_GuildId ON CraftingProfiles (UserId, GuildId);

CREATE TABLE IF NOT EXISTS PlayerInventoryItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    ItemName TEXT, ItemType TEXT, Quantity INTEGER NOT NULL DEFAULT 0,
    Rarity TEXT DEFAULT 'Common', DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_PlayerInventoryItems_UserId_GuildId ON PlayerInventoryItems (UserId, GuildId);

CREATE TABLE IF NOT EXISTS PvpStats (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    Wins INTEGER NOT NULL DEFAULT 0, Losses INTEGER NOT NULL DEFAULT 0,
    Draws INTEGER NOT NULL DEFAULT 0, Elo INTEGER NOT NULL DEFAULT 1000,
    WinStreak INTEGER NOT NULL DEFAULT 0, BestWinStreak INTEGER NOT NULL DEFAULT 0,
    TotalDamageDealt INTEGER NOT NULL DEFAULT 0, TotalDamageReceived INTEGER NOT NULL DEFAULT 0,
    LastDuelAt TEXT, DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_PvpStats_UserId_GuildId ON PvpStats (UserId, GuildId);

CREATE TABLE IF NOT EXISTS Tournaments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChannelId INTEGER NOT NULL DEFAULT 0,
    Name TEXT, Status TEXT DEFAULT 'Registration',
    Format TEXT DEFAULT 'SingleElimination',
    MaxParticipants INTEGER NOT NULL DEFAULT 16,
    EntryFee INTEGER NOT NULL DEFAULT 0, PrizePool INTEGER NOT NULL DEFAULT 0,
    CreatedBy INTEGER NOT NULL DEFAULT 0,
    StartedAt TEXT, CompletedAt TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS TournamentParticipants (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TournamentId INTEGER NOT NULL, UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    Seed INTEGER NOT NULL DEFAULT 0, IsEliminated INTEGER NOT NULL DEFAULT 0,
    Wins INTEGER NOT NULL DEFAULT 0, Losses INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS PlayerHouses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    HouseName TEXT, HouseSize TEXT DEFAULT 'Cottage',
    Level INTEGER NOT NULL DEFAULT 1, RoomCount INTEGER NOT NULL DEFAULT 1,
    GardenSize INTEGER NOT NULL DEFAULT 0, TrophySlots INTEGER NOT NULL DEFAULT 0,
    GuestBookEntries INTEGER NOT NULL DEFAULT 0,
    LastVisitedBy INTEGER NOT NULL DEFAULT 0,
    PurchasePrice INTEGER NOT NULL DEFAULT 0, Style TEXT DEFAULT 'Medieval', DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_PlayerHouses_UserId_GuildId ON PlayerHouses (UserId, GuildId);

CREATE TABLE IF NOT EXISTS HouseRooms (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    HouseId INTEGER NOT NULL, RoomName TEXT, RoomType TEXT,
    FurnitureCount INTEGER NOT NULL DEFAULT 0, Decorations TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS HouseFurniture (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    HouseId INTEGER NOT NULL, RoomName TEXT, ItemName TEXT,
    ItemType TEXT, Rarity TEXT DEFAULT 'Common', BonusEffect TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS GuestBookEntries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    HouseId INTEGER NOT NULL, VisitorUserId INTEGER NOT NULL,
    VisitorName TEXT, Message TEXT,
    VisitedAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS LoreEntries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Category TEXT, EntryName TEXT,
    Description TEXT, Emoji TEXT, IsDiscovered INTEGER NOT NULL DEFAULT 0,
    DiscoveredBy INTEGER NOT NULL DEFAULT 0, DiscoveredAt TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS PlayerDiscoveries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    LoreEntryId INTEGER NOT NULL, DiscoveredAt TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS TreasureMaps (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, UserId INTEGER NOT NULL,
    MapName TEXT, Clue1 TEXT, Clue2 TEXT, Clue3 TEXT,
    RewardCurrency INTEGER NOT NULL DEFAULT 0, RewardXp INTEGER NOT NULL DEFAULT 0,
    RewardItemName TEXT, IsSolved INTEGER NOT NULL DEFAULT 0,
    SolvedBy INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    ExpiresAt TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS WorldEvents (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, EventName TEXT,
    EventType TEXT, Description TEXT, IsActive INTEGER NOT NULL DEFAULT 0,
    StartedAt TEXT, EndsAt TEXT, BonusEffect TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS ServerEvents (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChannelId INTEGER NOT NULL DEFAULT 0,
    CreatedBy INTEGER NOT NULL DEFAULT 0,
    Title TEXT, Description TEXT, EventType TEXT DEFAULT 'Custom',
    StartTime TEXT NOT NULL, EndTime TEXT,
    IsRecurring INTEGER NOT NULL DEFAULT 0, RecurrencePattern TEXT,
    MaxAttendees INTEGER NOT NULL DEFAULT 0,
    Status TEXT DEFAULT 'Upcoming', DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS EventRsvps (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventId INTEGER NOT NULL, UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    Status TEXT DEFAULT 'Going',
    RsvpAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS EventReminders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    ReminderMinutesBefore INTEGER NOT NULL DEFAULT 15,
    Sent INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS MovieNightPolls (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChannelId INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedBy INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS MovieNightOptions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PollId INTEGER NOT NULL, MovieTitle TEXT,
    AddedBy INTEGER NOT NULL DEFAULT 0,
    Votes INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS BotCustomizations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, CurrencyName TEXT DEFAULT 'Currency',
    CurrencyEmoji TEXT DEFAULT '🥠', XpName TEXT DEFAULT 'XP',
    XpEmoji TEXT DEFAULT '⭐', EmbedColorHex TEXT DEFAULT '#00E68A',
    EmbedFooterText TEXT, EmbedThumbnailUrl TEXT,
    SuccessPrefix TEXT DEFAULT '✅', ErrorPrefix TEXT DEFAULT '❌',
    LevelUpMessage TEXT, LevelUpChannelId INTEGER NOT NULL DEFAULT 0,
    LevelUpDm INTEGER NOT NULL DEFAULT 0,
    WelcomeTitle TEXT, WelcomeMessage TEXT, WelcomeImageUrl TEXT,
    GoodbyeMessage TEXT, ActiveTheme TEXT DEFAULT 'default', DateAdded TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_BotCustomizations_GuildId ON BotCustomizations (GuildId);

CREATE TABLE IF NOT EXISTS CustomEmbeds (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Name TEXT, Title TEXT, Description TEXT,
    ColorHex TEXT, ThumbnailUrl TEXT, ImageUrl TEXT,
    FooterText TEXT, AuthorName TEXT, Fields TEXT,
    CreatedBy INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS CustomCommands (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Trigger TEXT, Response TEXT,
    EmbedName TEXT, IsEmbed INTEGER NOT NULL DEFAULT 0,
    CreatedBy INTEGER NOT NULL DEFAULT 0,
    UseCount INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS SoundboardSounds (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Name TEXT, Url TEXT,
    AddedBy INTEGER NOT NULL DEFAULT 0, PlayCount INTEGER NOT NULL DEFAULT 0,
    Category TEXT DEFAULT 'General', DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS TempVoiceConfigs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, CreateChannelId INTEGER NOT NULL DEFAULT 0,
    CategoryId INTEGER NOT NULL DEFAULT 0,
    DefaultName TEXT DEFAULT '{user}''s Channel',
    DefaultLimit INTEGER NOT NULL DEFAULT 0,
    IsEnabled INTEGER NOT NULL DEFAULT 1, DateAdded TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_TempVoiceConfigs_GuildId ON TempVoiceConfigs (GuildId);

CREATE TABLE IF NOT EXISTS TempVoiceChannels (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChannelId INTEGER NOT NULL,
    OwnerId INTEGER NOT NULL, Name TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS VoiceSessionLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    ChannelId INTEGER NOT NULL,
    JoinedAt TEXT NOT NULL, LeftAt TEXT,
    DurationMinutes INTEGER NOT NULL DEFAULT 0,
    WasStreaming INTEGER NOT NULL DEFAULT 0,
    WasMuted INTEGER NOT NULL DEFAULT 0,
    WasDeafened INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_VoiceSessionLogs_UserId_GuildId ON VoiceSessionLogs (UserId, GuildId);

CREATE TABLE IF NOT EXISTS StreamAlerts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, AlertChannelId INTEGER NOT NULL DEFAULT 0,
    StreamerUserId INTEGER NOT NULL DEFAULT 0,
    Platform TEXT DEFAULT 'Discord', CustomMessage TEXT,
    IsEnabled INTEGER NOT NULL DEFAULT 1, LastAlertAt TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS ContentSchedules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, UserId INTEGER NOT NULL,
    Title TEXT, Platform TEXT, Description TEXT,
    ScheduledAt TEXT NOT NULL, IsCompleted INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS ChannelPointsConfigs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, PointsName TEXT DEFAULT 'Channel Points',
    PointsEmoji TEXT DEFAULT '🔷',
    PointsPerMessage INTEGER NOT NULL DEFAULT 1,
    PointsPerMinuteVoice INTEGER NOT NULL DEFAULT 2,
    IsEnabled INTEGER NOT NULL DEFAULT 1, DateAdded TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_ChannelPointsConfigs_GuildId ON ChannelPointsConfigs (GuildId);

CREATE TABLE IF NOT EXISTS UserChannelPoints (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    Points INTEGER NOT NULL DEFAULT 0,
    TotalEarned INTEGER NOT NULL DEFAULT 0,
    TotalSpent INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_UserChannelPoints_UserId_GuildId ON UserChannelPoints (UserId, GuildId);

CREATE TABLE IF NOT EXISTS ChannelPointRewards (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Name TEXT, Description TEXT,
    Cost INTEGER NOT NULL DEFAULT 0, RewardType TEXT,
    RoleId INTEGER NOT NULL DEFAULT 0,
    MaxRedemptions INTEGER NOT NULL DEFAULT 0,
    CurrentRedemptions INTEGER NOT NULL DEFAULT 0,
    IsEnabled INTEGER NOT NULL DEFAULT 1, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS Predictions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChannelId INTEGER NOT NULL DEFAULT 0,
    CreatedBy INTEGER NOT NULL DEFAULT 0,
    Question TEXT, Option1 TEXT, Option2 TEXT,
    Option1Points INTEGER NOT NULL DEFAULT 0, Option2Points INTEGER NOT NULL DEFAULT 0,
    Option1Voters INTEGER NOT NULL DEFAULT 0, Option2Voters INTEGER NOT NULL DEFAULT 0,
    Status TEXT DEFAULT 'Open', WinningOption INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS PredictionBets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PredictionId INTEGER NOT NULL, UserId INTEGER NOT NULL,
    ChosenOption INTEGER NOT NULL DEFAULT 0,
    PointsBet INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS FanArtSubmissions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, UserId INTEGER NOT NULL,
    Title TEXT, ImageUrl TEXT, Votes INTEGER NOT NULL DEFAULT 0,
    SubmittedAt TEXT NOT NULL DEFAULT (datetime('now')),
    IsApproved INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS FeedSubscriptions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChannelId INTEGER NOT NULL DEFAULT 0,
    FeedType TEXT, FeedUrl TEXT, FeedName TEXT,
    LastItemId TEXT, LastCheckedAt TEXT,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    AddedBy INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS UptimeMonitors (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, AlertChannelId INTEGER NOT NULL DEFAULT 0,
    Url TEXT, Name TEXT, IsUp INTEGER NOT NULL DEFAULT 1,
    LastDownAt TEXT, LastCheckedAt TEXT,
    CheckIntervalMinutes INTEGER NOT NULL DEFAULT 5,
    ConsecutiveFailures INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS BotPlugins (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, PluginName TEXT, Description TEXT,
    Version TEXT, Author TEXT, IsEnabled INTEGER NOT NULL DEFAULT 1,
    InstalledAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS WebhookEndpoints (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Name TEXT, Secret TEXT,
    TargetChannelId TEXT, EventType TEXT,
    TriggerCount INTEGER NOT NULL DEFAULT 0,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS FeatureFlags (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, FeatureName TEXT,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    EnabledForRoles TEXT, RolloutPercent INTEGER NOT NULL DEFAULT 100,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS CommandLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, UserId INTEGER NOT NULL,
    ChannelId INTEGER NOT NULL, CommandName TEXT, Arguments TEXT,
    Success INTEGER NOT NULL DEFAULT 1,
    ExecutionMs INTEGER NOT NULL DEFAULT 0,
    ExecutedAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_CommandLogs_GuildId ON CommandLogs (GuildId);

CREATE TABLE IF NOT EXISTS XpMultipliers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Type TEXT, TargetId INTEGER NOT NULL DEFAULT 0,
    Multiplier REAL NOT NULL DEFAULT 1.0,
    ExpiresAt TEXT, IsActive INTEGER NOT NULL DEFAULT 1, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS XpChallengeEntries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChallengeName TEXT, Description TEXT,
    Requirement TEXT, XpReward INTEGER NOT NULL DEFAULT 0,
    StartsAt TEXT NOT NULL, EndsAt TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS XpChallengeParticipants (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ChallengeId INTEGER NOT NULL, UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    Progress INTEGER NOT NULL DEFAULT 0,
    IsComplete INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS SkillTrees (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    Class TEXT, SkillPoints INTEGER NOT NULL DEFAULT 0,
    Skill1Level INTEGER NOT NULL DEFAULT 0, Skill2Level INTEGER NOT NULL DEFAULT 0,
    Skill3Level INTEGER NOT NULL DEFAULT 0, Skill4Level INTEGER NOT NULL DEFAULT 0,
    Skill5Level INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_SkillTrees_UserId_GuildId ON SkillTrees (UserId, GuildId);

CREATE TABLE IF NOT EXISTS PrestigeData (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    PrestigeLevel INTEGER NOT NULL DEFAULT 0,
    PrestigeBonusPercent INTEGER NOT NULL DEFAULT 0,
    LastPrestigeAt TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS DungeonModifiers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ModifierName TEXT, Description TEXT,
    IsActive INTEGER NOT NULL DEFAULT 0,
    AtkMult REAL NOT NULL DEFAULT 1.0, DefMult REAL NOT NULL DEFAULT 1.0,
    HpMult REAL NOT NULL DEFAULT 1.0, XpMult REAL NOT NULL DEFAULT 1.0,
    LootMult REAL NOT NULL DEFAULT 1.0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS Bounties (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, TargetUserId INTEGER NOT NULL,
    PostedBy INTEGER NOT NULL DEFAULT 0,
    Amount INTEGER NOT NULL DEFAULT 0, Reason TEXT,
    IsClaimed INTEGER NOT NULL DEFAULT 0,
    ClaimedBy INTEGER NOT NULL DEFAULT 0,
    PostedAt TEXT NOT NULL DEFAULT (datetime('now')),
    ClaimedAt TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS TreasureHunts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, ChannelId INTEGER NOT NULL DEFAULT 0,
    HiddenWord TEXT, Reward INTEGER NOT NULL DEFAULT 0,
    IsFound INTEGER NOT NULL DEFAULT 0,
    FoundBy INTEGER NOT NULL DEFAULT 0,
    HiddenAt TEXT NOT NULL DEFAULT (datetime('now')), DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS MarriageExpansions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    PartnerId INTEGER NOT NULL DEFAULT 0,
    Anniversary TEXT, SharedCurrency INTEGER NOT NULL DEFAULT 0,
    SharedXp INTEGER NOT NULL DEFAULT 0,
    ChildCount INTEGER NOT NULL DEFAULT 0,
    FamilyName TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS Horoscopes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    ZodiacSign TEXT, LastReadingAt TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS GoalTrackers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    GoalName TEXT, Description TEXT,
    TargetValue INTEGER NOT NULL DEFAULT 0,
    CurrentValue INTEGER NOT NULL DEFAULT 0,
    IsComplete INTEGER NOT NULL DEFAULT 0,
    Deadline TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS ServerNewspapers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Edition INTEGER NOT NULL DEFAULT 1,
    Content TEXT, PublishedAt TEXT NOT NULL DEFAULT (datetime('now')),
    GeneratedBy INTEGER NOT NULL DEFAULT 0, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS PremiumGuilds (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL, Tier TEXT DEFAULT 'Free',
    ExpiresAt TEXT, Features TEXT DEFAULT '',
    MaxCustomCommands INTEGER NOT NULL DEFAULT 5,
    MaxSavedEmbeds INTEGER NOT NULL DEFAULT 3,
    MaxFeeds INTEGER NOT NULL DEFAULT 2,
    PremiumSince TEXT, DateAdded TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_PremiumGuilds_GuildId ON PremiumGuilds (GuildId);

CREATE TABLE IF NOT EXISTS QuestProgress (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    QuestId TEXT, QuestName TEXT, Description TEXT,
    Type TEXT DEFAULT 'Daily', Status TEXT DEFAULT 'Active',
    CurrentProgress INTEGER NOT NULL DEFAULT 0,
    RequiredProgress INTEGER NOT NULL DEFAULT 0,
    XpReward INTEGER NOT NULL DEFAULT 0,
    CurrencyReward INTEGER NOT NULL DEFAULT 0,
    StartedAt TEXT NOT NULL DEFAULT (datetime('now')),
    CompletedAt TEXT, ExpiresAt TEXT, DateAdded TEXT
);
CREATE INDEX IF NOT EXISTS IX_QuestProgress_UserId_GuildId ON QuestProgress (UserId, GuildId);

CREATE TABLE IF NOT EXISTS QuestLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    TotalQuestsCompleted INTEGER NOT NULL DEFAULT 0,
    DailyQuestsCompleted INTEGER NOT NULL DEFAULT 0,
    WeeklyQuestsCompleted INTEGER NOT NULL DEFAULT 0,
    StoryQuestsCompleted INTEGER NOT NULL DEFAULT 0,
    CurrentStreak INTEGER NOT NULL DEFAULT 0,
    BestStreak INTEGER NOT NULL DEFAULT 0,
    LastDailyRefresh TEXT, LastWeeklyRefresh TEXT, DateAdded TEXT
);

CREATE TABLE IF NOT EXISTS FactionStandings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL, GuildId INTEGER NOT NULL,
    FactionName TEXT, Reputation INTEGER NOT NULL DEFAULT 0,
    Rank TEXT DEFAULT 'Outsider', DateAdded TEXT
);

COMMIT;
