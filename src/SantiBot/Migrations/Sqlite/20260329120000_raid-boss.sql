CREATE TABLE IF NOT EXISTS RaidBosses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL,
    ChannelId INTEGER NOT NULL,
    Name TEXT,
    Emoji TEXT,
    MaxHp INTEGER NOT NULL DEFAULT 0,
    CurrentHp INTEGER NOT NULL DEFAULT 0,
    Attack INTEGER NOT NULL DEFAULT 0,
    Defense INTEGER NOT NULL DEFAULT 0,
    CurrentPhase INTEGER NOT NULL DEFAULT 1,
    XpReward INTEGER NOT NULL DEFAULT 0,
    CurrencyReward INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SpawnedAt TEXT NOT NULL DEFAULT (datetime('now')),
    DefeatedAt TEXT,
    WasRandomSpawn INTEGER NOT NULL DEFAULT 0,
    DateAdded TEXT
);

CREATE INDEX IF NOT EXISTS IX_RaidBosses_GuildId_IsActive ON RaidBosses (GuildId, IsActive);

CREATE TABLE IF NOT EXISTS RaidBossParticipants (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RaidBossId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    GuildId INTEGER NOT NULL,
    DamageDealt INTEGER NOT NULL DEFAULT 0,
    HitCount INTEGER NOT NULL DEFAULT 0,
    LastAttackAt TEXT NOT NULL DEFAULT (datetime('now')),
    DateAdded TEXT
);

CREATE INDEX IF NOT EXISTS IX_RaidBossParticipants_RaidBossId ON RaidBossParticipants (RaidBossId);
CREATE INDEX IF NOT EXISTS IX_RaidBossParticipants_GuildId_UserId ON RaidBossParticipants (GuildId, UserId);

CREATE TABLE IF NOT EXISTS RaidBossConfigs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuildId INTEGER NOT NULL,
    RandomSpawnsEnabled INTEGER NOT NULL DEFAULT 1,
    MinDungeonClears INTEGER NOT NULL DEFAULT 5,
    MaxDungeonClears INTEGER NOT NULL DEFAULT 25,
    DungeonClearsSinceLastRaid INTEGER NOT NULL DEFAULT 0,
    NextSpawnThreshold INTEGER NOT NULL DEFAULT 10,
    SpawnChannelId INTEGER NOT NULL DEFAULT 0,
    DateAdded TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_RaidBossConfigs_GuildId ON RaidBossConfigs (GuildId);
