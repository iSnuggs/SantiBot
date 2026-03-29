#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games.Lore;

public sealed class LoreService(DbService _db, ICurrencyService _cs) : INService
{
    private static readonly SantiRandom _rng = new();

    // ───────────────────────── STATIC LORE DATA ─────────────────────────

    public static readonly List<(string Category, string Name, string Emoji, string Description)> DefaultLore =
    [
        // ── 15 Monsters ──
        ("Monster", "Shadow Rat", "\ud83d\udc00",
            "The smallest of dungeon vermin, shadow rats feed on ambient dark magic. Their eyes glow faintly red in total darkness, and they travel in swarms of hundreds. Many an adventurer has underestimated these creatures, only to be overwhelmed by sheer numbers."),
        ("Monster", "Bone Sentinel", "\ud83d\udc80",
            "Animated skeletons of fallen warriors, bone sentinels patrol the crypts endlessly. They retain fragments of muscle memory from their past lives and fight with eerie precision. Their hollow eye sockets burn with pale necromantic fire."),
        ("Monster", "Cave Troll", "\ud83e\uddcc",
            "Massive creatures that make their dens in the deepest dungeon chambers. Cave trolls regenerate wounds at an alarming rate and can only be permanently felled by fire or acid. Their stone-like skin deflects most mundane weapons."),
        ("Monster", "Plague Spider", "\ud83d\udd77\ufe0f",
            "Spiders the size of horses that spin webs laced with a slow-acting venom. Victims feel nothing at first, then collapse hours later in agonizing fever. Their webs are prized by alchemists for potion ingredients."),
        ("Monster", "Ember Elemental", "\ud83d\udd25",
            "Born from volcanic vents deep beneath the dungeon, ember elementals are living flames given crude intelligence. They drift through tunnels leaving scorch marks on the walls and ignite anything flammable within arm's reach."),
        ("Monster", "Frost Wraith", "\ud83e\uddca",
            "Ghosts of travelers who froze to death in the Iceveil Peaks. They haunt the frozen corridors and drain body heat from the living. Their touch causes instant frostbite, and the air drops to freezing within twenty paces of them."),
        ("Monster", "Venomfang Serpent", "\ud83d\udc0d",
            "An enormous snake that nests in flooded dungeon chambers. Its fangs inject a paralytic toxin that stops the heart in minutes. Ancient carvings suggest the serpent was once a temple guardian before the old religion fell."),
        ("Monster", "Iron Golem", "\ud83e\uddbf",
            "Constructs forged by long-dead artificers to guard treasure vaults. Iron golems follow their last orders with absolute loyalty, attacking anything that enters their designated zone. They feel no pain and never tire."),
        ("Monster", "Mushroom Shambler", "\ud83c\udf44",
            "A fungal creature that shambles through damp tunnels spreading toxic spores. Adventurers who breathe the spores hallucinate for hours, seeing monsters and allies swap places. Some scholars believe the shambler is actually a colony organism."),
        ("Monster", "Mimic Chest", "\ud83d\udce6",
            "A predator that disguises itself as a treasure chest. When unsuspecting looters reach inside, the mimic's adhesive tongue traps their arm while rows of wooden teeth clamp shut. Experienced dungeon-crawlers always poke chests with a ten-foot pole."),
        ("Monster", "Shadowclaw Assassin", "\ud83d\udde1\ufe0f",
            "Humanoid creatures that dwell in absolute darkness. They can flatten their bodies to slip through cracks no wider than a coin. Their claws are coated in a numbing poison so victims don't feel the first three cuts."),
        ("Monster", "Wailing Banshee", "\ud83d\udc7b",
            "The tormented spirit of a betrayed queen whose scream can shatter glass and rupture eardrums. She appears only at midnight, drifting through walls and floors. Those who hear her full wail and survive are left deaf for a week."),
        ("Monster", "Lava Crawler", "\ud83e\uddde",
            "Insectoid creatures with obsidian carapaces that swim through molten rock. They surface to ambush adventurers near volcanic fissures, dragging victims into the lava. Their shells retain heat for hours after leaving magma."),
        ("Monster", "Storm Harpy", "\u26a1",
            "Winged predators that ride thunderstorms into the upper dungeon levels. Their feathers crackle with static electricity, and they can call down localized lightning bolts. Storm harpies are fiercely territorial and attack in coordinated dive-bomb formations."),
        ("Monster", "Crystal Basilisk", "\ud83d\udc8e",
            "A reptile whose gaze crystallizes living flesh into gemstone. Victims become beautiful, tragic statues of sapphire and ruby. The basilisk's crystalline scales reflect light in dazzling patterns that mesmerize prey before the killing gaze lands."),

        // ── 10 Items ──
        ("Item", "Starfall Blade", "\u2694\ufe0f",
            "A longsword forged from a meteorite that crashed into the Ashen Wastes three centuries ago. The blade glows faintly blue in moonlight and cuts through magical barriers as easily as cloth. Only five were ever made, and three are lost."),
        ("Item", "Aegis of the Fallen King", "\ud83d\udee1\ufe0f",
            "A tower shield carried by King Aldric the Unbroken during the Siege of Thornwall. It absorbed a dragon's flame breath without a scratch. The shield hums when undead are near, a remnant of the holy blessing placed on it by war-priests."),
        ("Item", "Serpent's Fang Dagger", "\ud83d\udde1\ufe0f",
            "Carved from the actual fang of the Great Serpent Yith'ala, this dagger injects a magical venom that saps the target's willpower. Wielders report hearing whispered suggestions from the blade itself, urging them to strike."),
        ("Item", "Crown of Whispers", "\ud83d\udc51",
            "A circlet of black iron set with opals that lets the wearer hear the surface thoughts of nearby creatures. Prolonged use causes migraines, nosebleeds, and eventually madness. The last known wearer tore it from their head and threw it into a volcano."),
        ("Item", "Boots of the Windwalker", "\ud83d\udc62",
            "Enchanted leather boots that allow the wearer to walk on air as though climbing invisible stairs. Each step leaves a brief shimmer in the air. The enchantment fades after a hundred paces and needs an hour to recharge."),
        ("Item", "Heartstone Amulet", "\ud83d\udc9a",
            "A palm-sized emerald on a mithril chain that absorbs lethal blows. When the wearer would receive a killing strike, the amulet shatters instead, releasing a burst of healing energy. It can only save one life before crumbling to dust."),
        ("Item", "Grimoire of Endless Night", "\ud83d\udcd6",
            "A spellbook bound in shadow-drake leather whose pages rewrite themselves under moonlight. It contains dark incantations that can extinguish all light in a mile radius. Reading too many pages in one sitting invites nightmares for months."),
        ("Item", "Phoenix Feather Cloak", "\ud83e\udea8",
            "Woven from the tail feathers of a greater phoenix, this cloak grants immunity to fire and radiates comforting warmth. Once per day, the wearer can erupt in cleansing flame that heals allies and burns enemies."),
        ("Item", "Titan's Gauntlet", "\ud83e\uddbf",
            "A single iron gauntlet etched with giant runes that multiplies the wearer's grip strength tenfold. Warriors have used it to crush enemy weapons barehanded. The gauntlet slowly fuses with the wearer's arm if worn too long."),
        ("Item", "Voidstone Ring", "\ud83d\udc8d",
            "A ring cut from a stone found in the Abyssal Rift, where reality frays at the edges. It allows the wearer to blink short distances through solid matter. Overuse causes parts of the wearer to temporarily phase out of existence."),

        // ── 10 Locations ──
        ("Location", "Thornwall Keep", "\ud83c\udff0",
            "Once the seat of power for the Aldric Dynasty, Thornwall Keep fell during the Demon War and now serves as the dungeon's main entrance. Its crumbling towers are overgrown with thorned vines that move on their own, ensnaring trespassers."),
        ("Location", "The Sunken Bazaar", "\ud83c\udfea",
            "An underground marketplace built by a forgotten civilization. Flooded knee-deep in brackish water, its stalls still hold remnants of exotic goods. Merchants sometimes set up temporary shops here, selling to adventurers between dungeon runs."),
        ("Location", "Iceveil Peaks", "\ud83c\udfd4\ufe0f",
            "Frozen mountain passages above the dungeon where blizzards rage year-round. The ice here is ancient, thousands of years old, and contains frozen creatures from a previous age. Occasionally one thaws out and causes havoc."),
        ("Location", "The Ashen Wastes", "\ud83c\udf0b",
            "A volcanic wasteland on the dungeon's lowest level where the ground cracks and lava seeps through. Rare ore deposits and fire-resistant herbs grow here, drawing brave harvesters. The air shimmers with heat and smells of sulfur."),
        ("Location", "Whispering Crypts", "\u26b0\ufe0f",
            "Miles of burial chambers carved into limestone where the old kings sleep. The walls are inscribed with protective wards that have weakened over centuries. At night, the crypts echo with unintelligible whispers that drive the unprepared to panic."),
        ("Location", "Crystalvein Mines", "\u26cf\ufe0f",
            "Abandoned mining tunnels where veins of magical crystal still pulse with raw energy. The crystals emit a low hum that skilled mages can attune to for power. Cave-ins are common, and the crystal basilisks make their dens here."),
        ("Location", "Eldergrove Sanctuary", "\ud83c\udf33",
            "A vast underground forest lit by bioluminescent fungi. The trees here grow without sunlight, fed by magical ley lines. Druids once used this place as a meditation retreat. Now wild beasts and territorial fey guard its paths."),
        ("Location", "The Abyssal Rift", "\ud83c\udf0c",
            "A bottomless chasm at the dungeon's deepest point where reality itself seems to unravel. Strange lights flicker in the darkness far below, and gravity behaves unpredictably near the edge. No one who has descended has returned."),
        ("Location", "Ironforge District", "\ud83d\udd28",
            "The remnants of a dwarven forge-city built into the dungeon walls. Its furnaces still burn with enchanted coals that never cool. Adventurers use the forges to repair and upgrade equipment, though the automated defense golems sometimes activate."),
        ("Location", "Moonlit Grotto", "\ud83c\udf19",
            "A hidden cave where an underground river opens into a moonlit pool, somehow reflecting a sky that shouldn't be visible underground. The water has healing properties, and resting here restores energy faster than anywhere else in the dungeon."),

        // ── 5 Bosses ──
        ("Boss", "Vorgarth the Undying", "\ud83d\udc79",
            "Once a mortal king who bargained with a demon lord for immortality, Vorgarth was cursed to rule an undead army for eternity. He sits upon a throne of fused bones in the deepest crypt, commanding legions of skeletal warriors. Each time he is slain, he reforms within a week, forever bound to his cursed crown. His attacks drain life force, healing himself with every blow landed."),
        ("Boss", "Queen Arachnia", "\ud83d\udd77\ufe0f",
            "The mother of all plague spiders, Queen Arachnia is a nightmarish creature the size of a house. She was once an elven sorceress who experimented with spider venom and transformed herself permanently. Her web fills an entire dungeon floor, and she commands thousands of spider offspring. Her venom can dissolve armor, and she can sense vibrations through her web from a mile away."),
        ("Boss", "The Forge Titan", "\ud83e\uddbf",
            "The masterwork creation of the ancient dwarven artificers, the Forge Titan was built to protect Ironforge District. Standing forty feet tall, it wields a hammer that causes earthquakes and breathes superheated steam. Its iron body is virtually indestructible, but a hidden rune on its back can temporarily shut it down if struck precisely. It has guarded the forges for over a thousand years."),
        ("Boss", "Xyranthos the Storm Dragon", "\ud83d\udc09",
            "The last storm dragon, Xyranthos lairs in the caverns above Iceveil Peaks. His scales shimmer between blue and purple, crackling with electrical energy. His breath weapon is a concentrated bolt of lightning that can melt stone. Despite his ferocity, Xyranthos is intelligent and will sometimes parley with adventurers who bring worthy tribute. He hoards magical artifacts, not gold."),
        ("Boss", "The Void Weaver", "\ud83c\udf0c",
            "An entity from beyond the Abyssal Rift that has slowly been pulling itself into this reality. The Void Weaver has no fixed form, appearing as a shifting mass of darkness studded with cold white stars. It warps space around itself, teleporting attackers randomly and creating gravity wells. Its true name, if spoken backward, is said to banish it temporarily. No one has ever defeated it permanently."),

        // ── 5 NPCs ──
        ("NPC", "Grimjaw the Merchant", "\ud83e\uddd4",
            "A scarred half-orc who runs a supply shop in the Sunken Bazaar. Grimjaw lost his left eye and three fingers to a mimic twenty years ago but still laughs about it. He offers fair prices and occasionally shares dungeon tips with adventurers he likes. He secretly funds orphanages on the surface with his profits."),
        ("NPC", "Sage Elyndra", "\ud83e\uddd9\u200d\u2640\ufe0f",
            "An ancient elven scholar who studies the dungeon's magical phenomena from a warded library in Eldergrove Sanctuary. She has catalogued over three thousand species of dungeon creatures and can identify any monster from a single scale or feather. She offers quests to retrieve rare specimens and rewards handsomely."),
        ("NPC", "Commander Voss", "\ud83d\udc82",
            "The stern leader of the Dungeon Wardens, a militia that maintains order at Thornwall Keep. Voss is a retired adventurer who lost her adventuring party to Vorgarth and now dedicates her life to preparing others. She assigns bounties on dangerous monsters and organizes raid parties against bosses."),
        ("NPC", "Ratchet the Tinker", "\ud83d\udd27",
            "A gnome inventor who maintains a workshop in Ironforge District, jury-rigging equipment from salvaged dungeon parts. His creations are unpredictable but occasionally brilliant. He built a mechanical bird that maps unexplored tunnels and a portable bridge that folds into a pocket. He talks to his inventions as if they can hear him."),
        ("NPC", "The Ferryman", "\ud83d\udea3",
            "A hooded figure who operates a small boat on the underground river between Moonlit Grotto and the Abyssal Rift. No one has ever seen their face, and they speak in riddles. The Ferryman charges no gold, only stories. The more interesting the tale, the further they will take you. Some believe the Ferryman is the dungeon itself, given form."),

        // ── 5 History ──
        ("History", "The Founding of Thornwall", "\ud83d\udcdc",
            "Six hundred years ago, King Aldric I built Thornwall Keep atop an ancient ruin, not knowing that a vast dungeon network lay beneath. When miners broke through the cellar floor, they discovered the first level of what would become the most dangerous labyrinth on the continent. Aldric declared the dungeon a royal resource and sent expeditions to map it."),
        ("History", "The Demon War", "\u2694\ufe0f",
            "Three centuries ago, a cult opened a portal to the Abyss in the dungeon's depths, unleashing a demonic invasion. The Aldric Dynasty's army fought for seven years to seal the rift. King Aldric VII sacrificed himself to close the portal, but not before demons corrupted the lower levels permanently. Thornwall Keep was abandoned in the aftermath."),
        ("History", "The Dwarven Exodus", "\u26cf\ufe0f",
            "Five centuries ago, the dwarven clan Ironheart discovered rich crystal veins in the dungeon and built Ironforge District. They thrived for two centuries until the crystal veins attracted the Forge Titan, which they created to protect their home. When the Titan malfunctioned, it turned on its creators. The surviving dwarves fled, sealing the district behind enchanted doors."),
        ("History", "The Elven Sanctuary", "\ud83c\udf3f",
            "Over a thousand years ago, elven druids discovered the underground forest now called Eldergrove Sanctuary. They recognized it as a convergence of ley lines and built a meditation retreat. For centuries it was a place of peace until the Demon War corrupted nearby tunnels. The druids withdrew, leaving behind their library and wards, which Sage Elyndra later claimed."),
        ("History", "The Awakening of the Rift", "\ud83c\udf0c",
            "Fifty years ago, an earthquake split the dungeon's deepest floor, revealing the Abyssal Rift. Strange phenomena began immediately: gravity anomalies, time distortions, and sightings of the Void Weaver. The Dungeon Wardens established a permanent watch at the Rift's edge. Since then, the dungeon's monsters have grown stronger, as if the Rift is feeding them energy from beyond."),
    ];

    // ───────────────────────── WORLD EVENT TEMPLATES ─────────────────────────

    private static readonly List<(string Name, string Type, string Description, int DurationHours, string Bonus)> _eventTemplates =
    [
        ("Goblin Invasion", "Invasion",
            "Hordes of goblins have poured out of the lower tunnels! Monster encounters give double loot.",
            4, "2x currency from monsters"),
        ("Harvest Moon Festival", "Festival",
            "The Sunken Bazaar is celebrating the Harvest Moon! All shop prices are halved.",
            6, "50% shop discount"),
        ("Blood Eclipse", "Eclipse",
            "A crimson eclipse darkens the sky above Thornwall. Dark creatures grow stronger but drop rare materials.",
            3, "Rare drop chance +50%"),
        ("Thunder Tempest", "Storm",
            "A magical storm rages through the upper levels, supercharging all lightning attacks.",
            2, "2x XP from combat"),
        ("Blightspore Outbreak", "Plague",
            "Toxic spores from the Mushroom Shambler breeding grounds have spread. Poison damage is doubled for all creatures.",
            5, "Poison damage 2x (enemies and players)"),
        ("Dragon's Blessing", "Blessing",
            "Xyranthos the Storm Dragon has granted a rare blessing upon worthy adventurers. All healing is doubled.",
            4, "2x healing effectiveness"),
        ("Undead Rising", "Invasion",
            "Vorgarth has rallied his undead army! Skeletons and wraiths spawn in every corridor.",
            3, "3x monster spawns, 2x XP"),
        ("Crystal Resonance", "Blessing",
            "The crystal veins in the mines are pulsing with energy, empowering all magical equipment.",
            6, "+25% all stats from equipment"),
        ("Void Surge", "Eclipse",
            "The Abyssal Rift has widened, and reality warps around it. Strange portals appear throughout the dungeon.",
            2, "Random teleports, bonus treasure rooms"),
        ("Midsummer Gala", "Festival",
            "The Dungeon Wardens are hosting a celebration! Completing any quest earns bonus rewards.",
            8, "Quest rewards +100%"),
    ];

    // ───────────────────────── TREASURE MAP TEMPLATES ─────────────────────────

    private static readonly List<(string MapName, string Clue1, string Clue2, string Clue3, string Answer, string ItemReward)> _mapTemplates =
    [
        ("Aldric's Lost Treasury",
            "Seek where the thorned king once sat upon his seat of power.",
            "Below the broken crown, where moonlight never reaches.",
            "The answer lies in the name of the fallen keep.",
            "thornwall", "Starfall Blade Shard"),
        ("The Serpent's Hoard",
            "Follow the river where no sun reflects.",
            "The guardian sleeps coiled around riches older than the dungeon.",
            "Name the great serpent from the old temple.",
            "venomfang", "Serpent Scale Armor"),
        ("Ironheart Cache",
            "The dwarves hid their greatest treasure before they fled.",
            "Look where hammers once rang and furnaces still burn.",
            "What district did the dwarves call home?",
            "ironforge", "Titan's Gauntlet Fragment"),
        ("Druid's Secret Vault",
            "Where trees grow without sunlight and fungi glow like stars.",
            "The elves left more than books in their sanctuary.",
            "Name the underground forest.",
            "eldergrove", "Phoenix Feather Cloak Scrap"),
        ("Void Pilgrim's Offering",
            "At the edge of everything, where reality thins to nothing.",
            "The ferryman knows the way, but only stories pay the toll.",
            "What endless chasm lies at the dungeon's deepest point?",
            "rift", "Voidstone Ring Chip"),
        ("Ghost King's Regalia",
            "The undying monarch keeps his crown close, even in un-death.",
            "His throne room echoes with the marching of skeletal legions.",
            "Name the boss who bargained with demons for eternal life.",
            "vorgarth", "Crown of Whispers Fragment"),
        ("Arachnia's Silk Cache",
            "In webs thick enough to trap a giant, the spider queen nests.",
            "She was once an elf, transformed by her own ambition.",
            "What is the spider queen's title?",
            "arachnia", "Spider Silk Cloak"),
        ("Frostbound Relic",
            "Where ancient ice entombs creatures from a forgotten age.",
            "The peaks howl with eternal blizzards above the dungeon.",
            "Name the frozen mountain region.",
            "iceveil", "Frost Crystal"),
        ("Bazaar Merchant's Secret",
            "Knee-deep in water, traders hawk wares from a sunken civilization.",
            "One merchant lost an eye and three fingers but kept smiling.",
            "Name the half-orc shopkeeper.",
            "grimjaw", "Merchant's Lucky Coin"),
        ("Crystalvein Mother Lode",
            "Where gemstone veins hum with raw magical energy.",
            "The basilisk guards the richest deposit of all.",
            "Name the mine where magical crystals grow.",
            "crystalvein", "Uncut Power Crystal"),
    ];

    // ───────────────────────── LORE DISCOVERY ─────────────────────────

    /// <summary>
    /// Seeds default lore entries for a guild if none exist.
    /// </summary>
    public async Task SeedLoreAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var count = await ctx.GetTable<LoreEntry>()
            .CountAsync(x => x.GuildId == guildId);

        if (count > 0)
            return;

        foreach (var (category, name, emoji, description) in DefaultLore)
        {
            ctx.Set<LoreEntry>().Add(new LoreEntry
            {
                GuildId = guildId,
                Category = category,
                EntryName = name,
                Description = description,
                Emoji = emoji,
                IsDiscovered = false,
                DiscoveredBy = 0,
                DiscoveredAt = null,
            });
        }

        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Mark a lore entry as discovered by a user. Returns the entry, or null if already discovered.
    /// </summary>
    public async Task<LoreEntry> DiscoverLoreAsync(ulong guildId, ulong userId, string entryName)
    {
        await SeedLoreAsync(guildId);
        await using var ctx = _db.GetDbContext();

        var entry = await ctx.GetTable<LoreEntry>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.EntryName == entryName);

        if (entry is null)
            return null;

        // Check if this user already discovered it
        var existing = await ctx.GetTable<PlayerDiscovery>()
            .FirstOrDefaultAsync(x => x.UserId == userId
                && x.GuildId == guildId
                && x.LoreEntryId == entry.Id);

        if (existing is not null)
            return null;

        // Record player discovery
        ctx.Set<PlayerDiscovery>().Add(new PlayerDiscovery
        {
            UserId = userId,
            GuildId = guildId,
            LoreEntryId = entry.Id,
            DiscoveredAt = DateTime.UtcNow,
        });

        // Mark as first discovery if not yet discovered
        if (!entry.IsDiscovered)
        {
            await ctx.GetTable<LoreEntry>()
                .Where(x => x.Id == entry.Id)
                .UpdateAsync(_ => new LoreEntry
                {
                    IsDiscovered = true,
                    DiscoveredBy = userId,
                    DiscoveredAt = DateTime.UtcNow,
                });

            entry.IsDiscovered = true;
            entry.DiscoveredBy = userId;
            entry.DiscoveredAt = DateTime.UtcNow;
        }

        await ctx.SaveChangesAsync();
        return entry;
    }

    /// <summary>
    /// Auto-discover monster lore when a player defeats a monster by name.
    /// </summary>
    public async Task<LoreEntry> DiscoverMonsterAsync(ulong guildId, ulong userId, string monsterName)
    {
        await SeedLoreAsync(guildId);
        await using var ctx = _db.GetDbContext();

        var entry = await ctx.GetTable<LoreEntry>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.Category == "Monster"
                && x.EntryName == monsterName);

        if (entry is null)
            return null;

        return await DiscoverLoreAsync(guildId, userId, monsterName);
    }

    /// <summary>
    /// Get all monster entries discovered by a user (the bestiary).
    /// </summary>
    public async Task<List<LoreEntry>> GetBestiaryAsync(ulong guildId, ulong userId)
    {
        await SeedLoreAsync(guildId);
        await using var ctx = _db.GetDbContext();

        var discoveredIds = await ctx.GetTable<PlayerDiscovery>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .Select(x => x.LoreEntryId)
            .ToListAsyncLinqToDB();

        return await ctx.GetTable<LoreEntry>()
            .Where(x => x.GuildId == guildId
                && x.Category == "Monster"
                && discoveredIds.Contains(x.Id))
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Get all lore entries discovered by a user, optionally filtered by category.
    /// </summary>
    public async Task<List<LoreEntry>> GetDiscoveredLoreAsync(ulong guildId, ulong userId, string category = null)
    {
        await SeedLoreAsync(guildId);
        await using var ctx = _db.GetDbContext();

        var discoveredIds = await ctx.GetTable<PlayerDiscovery>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .Select(x => x.LoreEntryId)
            .ToListAsyncLinqToDB();

        var query = ctx.GetTable<LoreEntry>()
            .Where(x => x.GuildId == guildId && discoveredIds.Contains(x.Id));

        if (!string.IsNullOrEmpty(category))
            query = query.Where(x => x.Category == category);

        return await query.ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Get all undiscovered entries for a user, optionally by category.
    /// </summary>
    public async Task<List<LoreEntry>> GetUndiscoveredAsync(ulong guildId, ulong userId, string category = null)
    {
        await SeedLoreAsync(guildId);
        await using var ctx = _db.GetDbContext();

        var discoveredIds = await ctx.GetTable<PlayerDiscovery>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .Select(x => x.LoreEntryId)
            .ToListAsyncLinqToDB();

        var query = ctx.GetTable<LoreEntry>()
            .Where(x => x.GuildId == guildId && !discoveredIds.Contains(x.Id));

        if (!string.IsNullOrEmpty(category))
            query = query.Where(x => x.Category == category);

        return await query.ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Get a single lore entry by name.
    /// </summary>
    public async Task<LoreEntry> GetLoreEntryAsync(ulong guildId, string entryName)
    {
        await SeedLoreAsync(guildId);
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<LoreEntry>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.EntryName == entryName);
    }

    /// <summary>
    /// Check if a user has discovered a specific entry.
    /// </summary>
    public async Task<bool> HasDiscoveredAsync(ulong guildId, ulong userId, string entryName)
    {
        await using var ctx = _db.GetDbContext();

        var entry = await ctx.GetTable<LoreEntry>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.EntryName == entryName);

        if (entry is null)
            return false;

        return await ctx.GetTable<PlayerDiscovery>()
            .AnyAsyncLinqToDB(x => x.UserId == userId
                && x.GuildId == guildId
                && x.LoreEntryId == entry.Id);
    }

    // ───────────────────────── TREASURE MAPS ─────────────────────────

    /// <summary>
    /// Generate a random treasure map for a user. Returns null if they already have an unsolved map.
    /// </summary>
    public async Task<TreasureMap> GenerateTreasureMapAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        // Check for existing unsolved map
        var existing = await ctx.GetTable<TreasureMap>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.UserId == userId
                && !x.IsSolved
                && x.ExpiresAt > DateTime.UtcNow);

        if (existing is not null)
            return null;

        var template = _mapTemplates[_rng.Next(0, _mapTemplates.Count)];
        var currencyReward = _rng.Next(500, 2001);
        var xpReward = _rng.Next(200, 1001);

        var map = new TreasureMap
        {
            GuildId = guildId,
            UserId = userId,
            MapName = template.MapName,
            Clue1 = template.Clue1,
            Clue2 = template.Clue2,
            Clue3 = template.Clue3,
            RewardCurrency = currencyReward,
            RewardXp = xpReward,
            RewardItemName = template.ItemReward,
            IsSolved = false,
            SolvedBy = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
        };

        ctx.Set<TreasureMap>().Add(map);
        await ctx.SaveChangesAsync();
        return map;
    }

    /// <summary>
    /// Attempt to solve a treasure map. Returns (success, map, error).
    /// </summary>
    public async Task<(bool success, TreasureMap map, string error)> SolveTreasureMapAsync(
        ulong guildId, ulong userId, string answer)
    {
        await using var ctx = _db.GetDbContext();

        var map = await ctx.GetTable<TreasureMap>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.UserId == userId
                && !x.IsSolved
                && x.ExpiresAt > DateTime.UtcNow);

        if (map is null)
            return (false, null, "no_map");

        // Find the matching template to check the answer
        var template = _mapTemplates.FirstOrDefault(t => t.MapName == map.MapName);
        if (template == default)
            return (false, null, "invalid_map");

        var normalizedAnswer = answer.Trim().ToLowerInvariant();
        var correctAnswer = template.Answer.ToLowerInvariant();

        if (!normalizedAnswer.Contains(correctAnswer))
            return (false, map, "wrong_answer");

        // Mark as solved
        await ctx.GetTable<TreasureMap>()
            .Where(x => x.Id == map.Id)
            .UpdateAsync(_ => new TreasureMap
            {
                IsSolved = true,
                SolvedBy = userId,
            });

        // Award currency
        await _cs.AddAsync(userId, map.RewardCurrency,
            new("lore", "treasure", $"Treasure map: {map.MapName}"));

        map.IsSolved = true;
        map.SolvedBy = userId;
        return (true, map, null);
    }

    /// <summary>
    /// Get all active (unsolved, unexpired) maps for a guild.
    /// </summary>
    public async Task<List<TreasureMap>> GetActiveMapsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<TreasureMap>()
            .Where(x => x.GuildId == guildId
                && !x.IsSolved
                && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Get a user's current active map, if any.
    /// </summary>
    public async Task<TreasureMap> GetUserMapAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<TreasureMap>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.UserId == userId
                && !x.IsSolved
                && x.ExpiresAt > DateTime.UtcNow);
    }

    // ───────────────────────── WORLD EVENTS ─────────────────────────

    /// <summary>
    /// Start a random world event for a guild. Returns null if max active events reached.
    /// </summary>
    public async Task<WorldEvent> StartWorldEventAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var activeCount = await ctx.GetTable<WorldEvent>()
            .CountAsync(x => x.GuildId == guildId
                && x.IsActive
                && x.EndsAt > DateTime.UtcNow);

        if (activeCount >= 3)
            return null;

        var template = _eventTemplates[_rng.Next(0, _eventTemplates.Count)];

        var worldEvent = new WorldEvent
        {
            GuildId = guildId,
            EventName = template.Name,
            EventType = template.Type,
            Description = template.Description,
            IsActive = true,
            StartedAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(template.DurationHours),
            BonusEffect = template.Bonus,
        };

        ctx.Set<WorldEvent>().Add(worldEvent);
        await ctx.SaveChangesAsync();
        return worldEvent;
    }

    /// <summary>
    /// Start a specific world event by name.
    /// </summary>
    public async Task<WorldEvent> StartWorldEventAsync(ulong guildId, string eventName)
    {
        var template = _eventTemplates.FirstOrDefault(t =>
            t.Name.Equals(eventName, StringComparison.OrdinalIgnoreCase));

        if (template == default)
            return null;

        await using var ctx = _db.GetDbContext();

        var worldEvent = new WorldEvent
        {
            GuildId = guildId,
            EventName = template.Name,
            EventType = template.Type,
            Description = template.Description,
            IsActive = true,
            StartedAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(template.DurationHours),
            BonusEffect = template.Bonus,
        };

        ctx.Set<WorldEvent>().Add(worldEvent);
        await ctx.SaveChangesAsync();
        return worldEvent;
    }

    /// <summary>
    /// Get all active world events for a guild.
    /// </summary>
    public async Task<List<WorldEvent>> GetActiveEventsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        // Auto-expire old events
        await ctx.GetTable<WorldEvent>()
            .Where(x => x.GuildId == guildId
                && x.IsActive
                && x.EndsAt <= DateTime.UtcNow)
            .UpdateAsync(_ => new WorldEvent { IsActive = false });

        return await ctx.GetTable<WorldEvent>()
            .Where(x => x.GuildId == guildId
                && x.IsActive
                && x.EndsAt > DateTime.UtcNow)
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// End a specific event early.
    /// </summary>
    public async Task<bool> EndEventAsync(ulong guildId, string eventName)
    {
        await using var ctx = _db.GetDbContext();

        var affected = await ctx.GetTable<WorldEvent>()
            .Where(x => x.GuildId == guildId
                && x.EventName == eventName
                && x.IsActive)
            .UpdateAsync(_ => new WorldEvent { IsActive = false });

        return affected > 0;
    }

    /// <summary>
    /// Check if a specific event type is active (for gameplay bonuses).
    /// </summary>
    public async Task<WorldEvent> GetActiveEventByTypeAsync(ulong guildId, string eventType)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<WorldEvent>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.EventType == eventType
                && x.IsActive
                && x.EndsAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Get discovery stats for a user.
    /// </summary>
    public async Task<(int discovered, int total, Dictionary<string, (int found, int max)> byCategory)>
        GetLoreStatsAsync(ulong guildId, ulong userId)
    {
        await SeedLoreAsync(guildId);
        await using var ctx = _db.GetDbContext();

        var allEntries = await ctx.GetTable<LoreEntry>()
            .Where(x => x.GuildId == guildId)
            .Select(x => new { x.Id, x.Category })
            .ToListAsyncLinqToDB();

        var discoveredIds = await ctx.GetTable<PlayerDiscovery>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .Select(x => x.LoreEntryId)
            .ToListAsyncLinqToDB();

        var total = allEntries.Count;
        var discovered = discoveredIds.Count;

        var byCategory = allEntries
            .GroupBy(x => x.Category)
            .ToDictionary(
                g => g.Key,
                g => (found: g.Count(e => discoveredIds.Contains(e.Id)), max: g.Count()));

        return (discovered, total, byCategory);
    }
}
