BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS "AutoFlows" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AutoFlows" PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Name" TEXT NULL,
    "FlowJson" TEXT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS "IX_AutoFlows_GuildId" ON "AutoFlows" ("GuildId");

CREATE TABLE IF NOT EXISTS "ChannelPrefixes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ChannelPrefixes" PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Prefix" TEXT NULL
);

CREATE INDEX IF NOT EXISTS "IX_ChannelPrefixes_GuildId" ON "ChannelPrefixes" ("GuildId");

CREATE TABLE IF NOT EXISTS "CustomScripts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CustomScripts" PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Trigger" TEXT NULL,
    "Script" TEXT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS "IX_CustomScripts_GuildId" ON "CustomScripts" ("GuildId");

COMMIT;
