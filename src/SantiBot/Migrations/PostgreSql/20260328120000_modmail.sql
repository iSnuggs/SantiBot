START TRANSACTION;

CREATE TABLE "ModMailConfig" (
    "Id" SERIAL PRIMARY KEY,
    "DateAdded" TIMESTAMP WITHOUT TIME ZONE NULL,
    "GuildId" NUMERIC(20,0) NOT NULL,
    "Enabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "CategoryId" NUMERIC(20,0) NULL,
    "LogChannelId" NUMERIC(20,0) NULL,
    "StaffRoleId" NUMERIC(20,0) NULL,
    "OpenMessage" TEXT NULL,
    "CloseMessage" TEXT NULL
);

CREATE TABLE "ModMailThread" (
    "Id" SERIAL PRIMARY KEY,
    "DateAdded" TIMESTAMP WITHOUT TIME ZONE NULL,
    "GuildId" NUMERIC(20,0) NOT NULL,
    "UserId" NUMERIC(20,0) NOT NULL,
    "ChannelId" NUMERIC(20,0) NOT NULL,
    "Status" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ClosedAt" TIMESTAMP WITHOUT TIME ZONE NULL,
    "ClosedByUserId" NUMERIC(20,0) NULL,
    "MessageCount" INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE "ModMailMessage" (
    "Id" SERIAL PRIMARY KEY,
    "DateAdded" TIMESTAMP WITHOUT TIME ZONE NULL,
    "ThreadId" INTEGER NOT NULL,
    "AuthorId" NUMERIC(20,0) NOT NULL,
    "IsStaff" BOOLEAN NOT NULL DEFAULT FALSE,
    "Content" TEXT NULL,
    "Attachments" TEXT NULL,
    "SentAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

CREATE TABLE "ModMailBlock" (
    "Id" SERIAL PRIMARY KEY,
    "DateAdded" TIMESTAMP WITHOUT TIME ZONE NULL,
    "GuildId" NUMERIC(20,0) NOT NULL,
    "UserId" NUMERIC(20,0) NOT NULL,
    "Reason" TEXT NULL,
    "BlockedByUserId" NUMERIC(20,0) NOT NULL,
    "BlockedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

CREATE UNIQUE INDEX "IX_ModMailConfig_GuildId" ON "ModMailConfig" ("GuildId");
CREATE INDEX "IX_ModMailThread_ChannelId" ON "ModMailThread" ("ChannelId");
CREATE INDEX "IX_ModMailThread_GuildId_UserId_Status" ON "ModMailThread" ("GuildId", "UserId", "Status");
CREATE INDEX "IX_ModMailMessage_ThreadId" ON "ModMailMessage" ("ThreadId");
CREATE UNIQUE INDEX "IX_ModMailBlock_GuildId_UserId" ON "ModMailBlock" ("GuildId", "UserId");

COMMIT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260328120000_modmail', '9.0.1');
