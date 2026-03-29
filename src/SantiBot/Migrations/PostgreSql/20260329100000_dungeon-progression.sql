BEGIN TRANSACTION;

-- Expand dungeonplayers with leveling, class, and equipment
ALTER TABLE dungeonplayers ADD COLUMN guildid BIGINT NOT NULL DEFAULT 0;
ALTER TABLE dungeonplayers ADD COLUMN level INTEGER NOT NULL DEFAULT 1;
ALTER TABLE dungeonplayers ADD COLUMN xp BIGINT NOT NULL DEFAULT 0;
ALTER TABLE dungeonplayers ADD COLUMN basehp INTEGER NOT NULL DEFAULT 100;
ALTER TABLE dungeonplayers ADD COLUMN baseattack INTEGER NOT NULL DEFAULT 20;
ALTER TABLE dungeonplayers ADD COLUMN basedefense INTEGER NOT NULL DEFAULT 10;
ALTER TABLE dungeonplayers ADD COLUMN class TEXT DEFAULT 'Adventurer';
ALTER TABLE dungeonplayers ADD COLUMN race TEXT DEFAULT 'Human';
ALTER TABLE dungeonplayers ADD COLUMN highestdifficulty INTEGER NOT NULL DEFAULT 0;
ALTER TABLE dungeonplayers ADD COLUMN deathcount INTEGER NOT NULL DEFAULT 0;
ALTER TABLE dungeonplayers ADD COLUMN weaponid INTEGER NOT NULL DEFAULT 0;
ALTER TABLE dungeonplayers ADD COLUMN armorid INTEGER NOT NULL DEFAULT 0;
ALTER TABLE dungeonplayers ADD COLUMN accessoryid INTEGER NOT NULL DEFAULT 0;

-- New dungeonitems table for equipment/inventory
CREATE TABLE IF NOT EXISTS dungeonitems (
    id SERIAL PRIMARY KEY,
    dateadded TIMESTAMP WITHOUT TIME ZONE,
    userid BIGINT NOT NULL,
    guildid BIGINT NOT NULL,
    name TEXT,
    slot TEXT,
    rarity TEXT DEFAULT 'Common',
    bonushp INTEGER NOT NULL DEFAULT 0,
    bonusattack INTEGER NOT NULL DEFAULT 0,
    bonusdefense INTEGER NOT NULL DEFAULT 0,
    specialeffect TEXT,
    isequipped BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS ix_dungeonitems_userid_guildid ON dungeonitems (userid, guildid);
CREATE INDEX IF NOT EXISTS ix_dungeonplayers_userid_guildid ON dungeonplayers (userid, guildid);

COMMIT;
