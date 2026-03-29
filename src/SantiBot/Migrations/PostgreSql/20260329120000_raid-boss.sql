CREATE TABLE IF NOT EXISTS "RaidBosses" (
    "Id" SERIAL PRIMARY KEY,
    "GuildId" BIGINT NOT NULL,
    "ChannelId" BIGINT NOT NULL,
    "Name" TEXT,
    "Emoji" TEXT,
    "MaxHp" BIGINT NOT NULL DEFAULT 0,
    "CurrentHp" BIGINT NOT NULL DEFAULT 0,
    "Attack" INTEGER NOT NULL DEFAULT 0,
    "Defense" INTEGER NOT NULL DEFAULT 0,
    "CurrentPhase" INTEGER NOT NULL DEFAULT 1,
    "XpReward" BIGINT NOT NULL DEFAULT 0,
    "CurrencyReward" BIGINT NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "SpawnedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    "DefeatedAt" TIMESTAMP WITHOUT TIME ZONE,
    "WasRandomSpawn" BOOLEAN NOT NULL DEFAULT FALSE,
    "DateAdded" TIMESTAMP WITHOUT TIME ZONE
);

CREATE INDEX IF NOT EXISTS "IX_RaidBosses_GuildId_IsActive" ON "RaidBosses" ("GuildId", "IsActive");

CREATE TABLE IF NOT EXISTS "RaidBossParticipants" (
    "Id" SERIAL PRIMARY KEY,
    "RaidBossId" INTEGER NOT NULL,
    "UserId" BIGINT NOT NULL,
    "GuildId" BIGINT NOT NULL,
    "DamageDealt" BIGINT NOT NULL DEFAULT 0,
    "HitCount" INTEGER NOT NULL DEFAULT 0,
    "LastAttackAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    "DateAdded" TIMESTAMP WITHOUT TIME ZONE
);

CREATE INDEX IF NOT EXISTS "IX_RaidBossParticipants_RaidBossId" ON "RaidBossParticipants" ("RaidBossId");
CREATE INDEX IF NOT EXISTS "IX_RaidBossParticipants_GuildId_UserId" ON "RaidBossParticipants" ("GuildId", "UserId");

CREATE TABLE IF NOT EXISTS "RaidBossConfigs" (
    "Id" SERIAL PRIMARY KEY,
    "GuildId" BIGINT NOT NULL,
    "RandomSpawnsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "MinDungeonClears" INTEGER NOT NULL DEFAULT 5,
    "MaxDungeonClears" INTEGER NOT NULL DEFAULT 15,
    "DungeonClearsSinceLastRaid" INTEGER NOT NULL DEFAULT 0,
    "NextSpawnThreshold" INTEGER NOT NULL DEFAULT 10,
    "SpawnChannelId" BIGINT NOT NULL DEFAULT 0,
    "DateAdded" TIMESTAMP WITHOUT TIME ZONE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_RaidBossConfigs_GuildId" ON "RaidBossConfigs" ("GuildId");
