BEGIN TRANSACTION;

-- Expand DungeonPlayers with leveling, class, and equipment
ALTER TABLE "DungeonPlayers" ADD COLUMN "GuildId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "DungeonPlayers" ADD COLUMN "Level" INTEGER NOT NULL DEFAULT 1;
ALTER TABLE "DungeonPlayers" ADD COLUMN "Xp" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "DungeonPlayers" ADD COLUMN "BaseHp" INTEGER NOT NULL DEFAULT 100;
ALTER TABLE "DungeonPlayers" ADD COLUMN "BaseAttack" INTEGER NOT NULL DEFAULT 20;
ALTER TABLE "DungeonPlayers" ADD COLUMN "BaseDefense" INTEGER NOT NULL DEFAULT 10;
ALTER TABLE "DungeonPlayers" ADD COLUMN "Class" TEXT NULL DEFAULT 'Adventurer';
ALTER TABLE "DungeonPlayers" ADD COLUMN "Race" TEXT NULL DEFAULT 'Human';
ALTER TABLE "DungeonPlayers" ADD COLUMN "HighestDifficulty" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "DungeonPlayers" ADD COLUMN "DeathCount" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "DungeonPlayers" ADD COLUMN "WeaponId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "DungeonPlayers" ADD COLUMN "ArmorId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "DungeonPlayers" ADD COLUMN "AccessoryId" INTEGER NOT NULL DEFAULT 0;

-- New DungeonItems table for equipment/inventory
CREATE TABLE IF NOT EXISTS "DungeonItems" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DungeonItems" PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "Slot" TEXT NULL,
    "Rarity" TEXT NULL DEFAULT 'Common',
    "BonusHp" INTEGER NOT NULL DEFAULT 0,
    "BonusAttack" INTEGER NOT NULL DEFAULT 0,
    "BonusDefense" INTEGER NOT NULL DEFAULT 0,
    "SpecialEffect" TEXT NULL,
    "IsEquipped" INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "IX_DungeonItems_UserId_GuildId" ON "DungeonItems" ("UserId", "GuildId");
CREATE INDEX IF NOT EXISTS "IX_DungeonPlayers_UserId_GuildId" ON "DungeonPlayers" ("UserId", "GuildId");

COMMIT;
