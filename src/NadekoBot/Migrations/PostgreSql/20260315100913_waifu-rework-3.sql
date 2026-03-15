START TRANSACTION;
DELETE FROM waifucycle;
DELETE FROM waifucyclesnapshot;
DELETE FROM waifupendingpayout;
UPDATE waifuinfo SET totalproduced = 0;

ALTER TABLE waifucycle DROP COLUMN fanpool;

ALTER TABLE waifucycle DROP COLUMN foodsnapshot;

ALTER TABLE waifucycle DROP COLUMN managerearnings;

ALTER TABLE waifucycle RENAME COLUMN waifuearnings TO returnscap;

ALTER TABLE waifucycle RENAME COLUMN totalreturns TO price;

ALTER TABLE waifucycle RENAME COLUMN moodsnapshot TO waifufeepercent;

ALTER TABLE waifucycle ALTER COLUMN processedat DROP NOT NULL;

ALTER TABLE waifucycle ADD managercutpercent double precision NOT NULL DEFAULT 0.0;

ALTER TABLE waifucycle ADD processed boolean NOT NULL DEFAULT FALSE;

ALTER TABLE bantemplates ADD disableunban boolean NOT NULL DEFAULT FALSE;

CREATE INDEX ix_waifuinfo_manageruserid ON waifuinfo (manageruserid);

CREATE INDEX ix_waifucycle_cyclenumber_processed ON waifucycle (cyclenumber, processed);

INSERT INTO "__EFMigrationsHistory" (migrationid, productversion)
VALUES ('20260315100913_waifu-rework-3', '9.0.1');

COMMIT;

