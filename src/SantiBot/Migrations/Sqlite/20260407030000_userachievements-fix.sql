-- Fix UserAchievements table — add missing columns that exist in model but not in DB
-- Model has: GuildId, AchievementName, Description, Emoji
-- DB only had: UserId, AchievementId, UnlockedAt

ALTER TABLE "UserAchievements" ADD COLUMN "GuildId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "UserAchievements" ADD COLUMN "AchievementName" TEXT NOT NULL DEFAULT '';
ALTER TABLE "UserAchievements" ADD COLUMN "Description" TEXT NOT NULL DEFAULT '';
ALTER TABLE "UserAchievements" ADD COLUMN "Emoji" TEXT NOT NULL DEFAULT '';
