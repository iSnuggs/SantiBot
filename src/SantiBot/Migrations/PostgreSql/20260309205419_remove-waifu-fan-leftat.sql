START TRANSACTION;
DROP INDEX ix_lineupuser_guildid_channelid_userid;

ALTER TABLE waifufan DROP COLUMN leftat;

INSERT INTO "__EFMigrationsHistory" (migrationid, productversion)
VALUES ('20260309205419_remove-waifu-fan-leftat', '9.0.1');

COMMIT;

