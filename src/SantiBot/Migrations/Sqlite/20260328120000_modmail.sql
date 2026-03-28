BEGIN TRANSACTION;

CREATE TABLE "ModMailConfig" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ModMailConfig" PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "Enabled" INTEGER NOT NULL DEFAULT 0,
    "CategoryId" INTEGER NULL,
    "LogChannelId" INTEGER NULL,
    "StaffRoleId" INTEGER NULL,
    "OpenMessage" TEXT NULL,
    "CloseMessage" TEXT NULL
);

CREATE TABLE "ModMailThread" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ModMailThread" PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TEXT NOT NULL,
    "ClosedAt" TEXT NULL,
    "ClosedByUserId" INTEGER NULL,
    "MessageCount" INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE "ModMailMessage" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ModMailMessage" PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "ThreadId" INTEGER NOT NULL,
    "AuthorId" INTEGER NOT NULL,
    "IsStaff" INTEGER NOT NULL DEFAULT 0,
    "Content" TEXT NULL,
    "Attachments" TEXT NULL,
    "SentAt" TEXT NOT NULL
);

CREATE TABLE "ModMailBlock" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ModMailBlock" PRIMARY KEY AUTOINCREMENT,
    "DateAdded" TEXT NULL,
    "GuildId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Reason" TEXT NULL,
    "BlockedByUserId" INTEGER NOT NULL,
    "BlockedAt" TEXT NOT NULL
);

CREATE UNIQUE INDEX "IX_ModMailConfig_GuildId" ON "ModMailConfig" ("GuildId");
CREATE INDEX "IX_ModMailThread_ChannelId" ON "ModMailThread" ("ChannelId");
CREATE INDEX "IX_ModMailThread_GuildId_UserId_Status" ON "ModMailThread" ("GuildId", "UserId", "Status");
CREATE INDEX "IX_ModMailMessage_ThreadId" ON "ModMailMessage" ("ThreadId");
CREATE UNIQUE INDEX "IX_ModMailBlock_GuildId_UserId" ON "ModMailBlock" ("GuildId", "UserId");

COMMIT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260328120000_modmail', '9.0.1');
