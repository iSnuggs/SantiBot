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
    "MaxDungeonClears" INTEGER NOT NULL DEFAULT 15,
    "DungeonClearsSinceLastRaid" INTEGER NOT NULL DEFAULT 0,
    "NextSpawnThreshold" INTEGER NOT NULL DEFAULT 10,
    "SpawnChannelId" INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RaidBossConfigs_GuildId" ON "RaidBossConfigs" ("GuildId");

COMMIT;
