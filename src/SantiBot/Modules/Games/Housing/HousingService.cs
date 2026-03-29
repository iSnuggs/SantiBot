#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Games.Housing;

public sealed class HousingService(DbService _db, ICurrencyService _cs) : INService
{
    private static readonly SantiRandom _rng = new();

    // ─── House Size Definitions ───

    public record HouseSizeDef(string Name, long Price, int MaxRooms, int BaseTrophySlots, int BaseGardenSize);

    public static readonly HouseSizeDef[] HouseSizes =
    [
        new("Cottage",   500,    2, 1, 1),
        new("Cabin",     2_000,  3, 2, 2),
        new("House",     8_000,  5, 3, 3),
        new("Manor",     25_000, 8, 5, 5),
        new("Mansion",   75_000, 12, 8, 8),
        new("Castle",    200_000, 20, 15, 12),
    ];

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, HouseSizeDef> _sizeMap = new(
        HouseSizes.Select(h => new KeyValuePair<string, HouseSizeDef>(h.Name.ToLowerInvariant(), h))
    );

    // ─── House Styles ───

    public static readonly string[] HouseStyles =
    [
        "Medieval", "Modern", "Japanese", "Victorian", "Rustic",
        "Fantasy", "Steampunk", "Underwater", "Treehouse", "Cloud",
    ];

    private static readonly HashSet<string> _validStyles = new(HouseStyles, StringComparer.OrdinalIgnoreCase);

    // ─── Room Type Definitions ───

    public record RoomTypeDef(string Name, string Description, long UnlockCost);

    public static readonly RoomTypeDef[] RoomTypes =
    [
        new("Bedroom",    "A cozy place to rest after adventures.",          200),
        new("Kitchen",    "Cook meals and brew potions here.",               350),
        new("Workshop",   "Tinker with gadgets and craft items.",            500),
        new("Library",    "Shelves of knowledge boost your wisdom.",         600),
        new("Trophy",     "Display your greatest achievements.",             800),
        new("Garden",     "Grow flowers and rare herbs outdoors.",           400),
        new("Aquarium",   "A shimmering tank of exotic aquatic life.",       750),
        new("Music",      "Fill your home with beautiful melodies.",         550),
        new("Storage",    "Extra space for all your collected treasures.",   300),
        new("Greenhouse", "Tropical plants thrive in this glass enclosure.", 900),
    ];

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, RoomTypeDef> _roomMap = new(
        RoomTypes.Select(r => new KeyValuePair<string, RoomTypeDef>(r.Name.ToLowerInvariant(), r))
    );

    // ─── Furniture Catalog ───

    public record FurnitureDef(string ItemName, string ItemType, string Rarity, long Price, string BonusEffect);

    public static readonly FurnitureDef[] FurnitureCatalog =
    [
        // Tables
        new("Wooden Table",        "Table",      "Common",    100,  ""),
        new("Oak Dining Table",    "Table",      "Uncommon",  300,  "+1 guest book slot"),
        new("Marble Banquet Table","Table",      "Rare",      900,  "+3 guest book slots"),
        // Chairs
        new("Wooden Chair",        "Chair",      "Common",    50,   ""),
        new("Velvet Armchair",     "Chair",      "Uncommon",  250,  ""),
        new("Throne of Glory",     "Chair",      "Epic",      5_000, "+2 trophy slots"),
        // Beds
        new("Straw Mattress",      "Bed",        "Common",    75,   ""),
        new("Feather Bed",         "Bed",        "Uncommon",  400,  ""),
        new("Royal Canopy Bed",    "Bed",        "Rare",      1_200, "+5% house value"),
        // Shelves
        new("Small Shelf",         "Shelf",      "Common",    60,   ""),
        new("Mahogany Bookcase",   "Shelf",      "Uncommon",  350,  "+1 library bonus"),
        new("Enchanted Archive",   "Shelf",      "Epic",      3_500, "+10% library bonus"),
        // Lamps
        new("Candle Lamp",         "Lamp",       "Common",    40,   ""),
        new("Crystal Chandelier",  "Lamp",       "Rare",      800,  "+1 room ambiance"),
        new("Aurora Lantern",      "Lamp",       "Epic",      4_000, "Rooms glow with color"),
        // Rugs
        new("Woven Rug",           "Rug",        "Common",    80,   ""),
        new("Persian Carpet",      "Rug",        "Uncommon",  450,  "+1 style points"),
        new("Flying Carpet",       "Rug",        "Legendary", 15_000, "+1 extra room slot"),
        // Paintings
        new("Landscape Painting",  "Painting",   "Common",    120,  ""),
        new("Portrait of Valor",   "Painting",   "Uncommon",  500,  "+1 trophy slot"),
        new("Masterwork Mural",    "Painting",   "Rare",      1_500, "+2 trophy slots"),
        new("Cosmic Canvas",       "Painting",   "Legendary", 20_000, "+5 trophy slots"),
        // Trophies
        new("Bronze Trophy",       "Trophy",     "Common",    200,  "+50 house value"),
        new("Silver Trophy",       "Trophy",     "Uncommon",  600,  "+150 house value"),
        new("Gold Trophy",         "Trophy",     "Rare",      1_800, "+500 house value"),
        new("Diamond Trophy",      "Trophy",     "Epic",      6_000, "+2000 house value"),
        // Plants
        new("Potted Fern",         "Plant",      "Common",    45,   ""),
        new("Bonsai Tree",         "Plant",      "Uncommon",  350,  "+1 garden size"),
        new("Enchanted Rose",      "Plant",      "Rare",      1_000, "+2 garden size"),
        new("World Tree Sapling",  "Plant",      "Legendary", 25_000, "+5 garden size"),
        // Aquarium items
        new("Goldfish Bowl",       "Aquarium",   "Common",    150,  ""),
        new("Coral Reef Tank",     "Aquarium",   "Rare",      2_000, "+3 aquarium beauty"),
        new("Deep Sea Aquarium",   "Aquarium",   "Epic",      8_000, "+10 aquarium beauty"),
        // Instruments
        new("Wooden Flute",        "Instrument", "Common",    100,  ""),
        new("Grand Piano",         "Instrument", "Rare",      3_000, "+5 music room bonus"),
        new("Celestial Harp",      "Instrument", "Legendary", 30_000, "+15 music room bonus"),
    ];

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, FurnitureDef> _furnitureMap = new(
        FurnitureCatalog.Select(f => new KeyValuePair<string, FurnitureDef>(f.ItemName.ToLowerInvariant(), f))
    );

    // ═══════════════════════════════════════════
    //  House CRUD
    // ═══════════════════════════════════════════

    public async Task<PlayerHouse> GetHouseAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PlayerHouse>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);
    }

    public async Task<(bool Success, string Error, PlayerHouse House)> BuyHouseAsync(ulong userId, ulong guildId, string sizeName)
    {
        var key = (sizeName ?? "cottage").ToLowerInvariant();
        if (!_sizeMap.TryGetValue(key, out var sizeDef))
            return (false, $"Invalid house size. Choose from: {string.Join(", ", HouseSizes.Select(h => h.Name))}", null);

        var existing = await GetHouseAsync(userId, guildId);
        if (existing is not null)
            return (false, "You already own a house! Use `.house upgrade` to upgrade it.", null);

        var removed = await _cs.RemoveAsync(userId, sizeDef.Price, new TxData("house", "buy", $"Bought a {sizeDef.Name}"));
        if (!removed)
            return (false, $"You need **{sizeDef.Price}** currency to buy a {sizeDef.Name}!", null);

        await using var ctx = _db.GetDbContext();
        var house = new PlayerHouse
        {
            UserId = userId,
            GuildId = guildId,
            HouseSize = sizeDef.Name,
            HouseName = $"{sizeDef.Name}",
            Level = 1,
            RoomCount = 1,
            TrophySlots = sizeDef.BaseTrophySlots,
            GardenSize = sizeDef.BaseGardenSize,
            PurchasePrice = sizeDef.Price,
            Style = "Medieval",
        };
        ctx.Set<PlayerHouse>().Add(house);
        await ctx.SaveChangesAsync();

        // Add a default bedroom
        var defaultRoom = new HouseRoom
        {
            HouseId = house.Id,
            RoomName = "Main Bedroom",
            RoomType = "Bedroom",
        };
        ctx.Set<HouseRoom>().Add(defaultRoom);
        await ctx.SaveChangesAsync();

        return (true, null, house);
    }

    public async Task<(bool Success, string Error, PlayerHouse House, string OldSize, string NewSize)> UpgradeHouseAsync(ulong userId, ulong guildId)
    {
        var house = await GetHouseAsync(userId, guildId);
        if (house is null)
            return (false, "You don't own a house yet! Use `.house buy` first.", null, null, null);

        var currentIndex = Array.FindIndex(HouseSizes, h => h.Name.Equals(house.HouseSize, StringComparison.OrdinalIgnoreCase));
        if (currentIndex < 0 || currentIndex >= HouseSizes.Length - 1)
            return (false, "Your house is already at maximum size (Castle)!", null, null, null);

        var nextSize = HouseSizes[currentIndex + 1];
        var upgradeCost = nextSize.Price - HouseSizes[currentIndex].Price;

        var removed = await _cs.RemoveAsync(userId, upgradeCost, new TxData("house", "upgrade", $"Upgraded to {nextSize.Name}"));
        if (!removed)
            return (false, $"You need **{upgradeCost}** currency to upgrade to a {nextSize.Name}!", null, null, null);

        var oldSize = house.HouseSize;

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<PlayerHouse>()
            .Where(x => x.Id == house.Id)
            .UpdateAsync(_ => new PlayerHouse
            {
                HouseSize = nextSize.Name,
                Level = house.Level + 1,
                TrophySlots = nextSize.BaseTrophySlots,
                GardenSize = nextSize.BaseGardenSize,
                PurchasePrice = house.PurchasePrice + upgradeCost,
            });

        house.HouseSize = nextSize.Name;
        house.Level = house.Level + 1;
        house.PurchasePrice += upgradeCost;

        return (true, null, house, oldSize, nextSize.Name);
    }

    // ═══════════════════════════════════════════
    //  Rooms
    // ═══════════════════════════════════════════

    public async Task<List<HouseRoom>> GetRoomsAsync(int houseId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<HouseRoom>()
            .Where(x => x.HouseId == houseId)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool Success, string Error, HouseRoom Room)> AddRoomAsync(ulong userId, ulong guildId, string roomType, string roomName)
    {
        var house = await GetHouseAsync(userId, guildId);
        if (house is null)
            return (false, "You don't own a house yet!", null);

        var typeKey = (roomType ?? "").ToLowerInvariant();
        if (!_roomMap.TryGetValue(typeKey, out var roomDef))
            return (false, $"Invalid room type. Choose from: {string.Join(", ", RoomTypes.Select(r => r.Name))}", null);

        var currentSizeDef = _sizeMap.GetValueOrDefault(house.HouseSize.ToLowerInvariant());
        if (currentSizeDef is null)
            return (false, "House data error.", null);

        var rooms = await GetRoomsAsync(house.Id);
        if (rooms.Count >= currentSizeDef.MaxRooms)
            return (false, $"Your {house.HouseSize} can only have **{currentSizeDef.MaxRooms}** rooms. Upgrade your house for more!", null);

        var removed = await _cs.RemoveAsync(userId, roomDef.UnlockCost, new TxData("house", "room", $"Added {roomDef.Name} room"));
        if (!removed)
            return (false, $"You need **{roomDef.UnlockCost}** currency to add a {roomDef.Name}!", null);

        var finalName = string.IsNullOrWhiteSpace(roomName) ? roomDef.Name : roomName;

        await using var ctx = _db.GetDbContext();
        var room = new HouseRoom
        {
            HouseId = house.Id,
            RoomName = finalName,
            RoomType = roomDef.Name,
        };
        ctx.Set<HouseRoom>().Add(room);
        await ctx.SaveChangesAsync();

        // Update room count
        await ctx.GetTable<PlayerHouse>()
            .Where(x => x.Id == house.Id)
            .UpdateAsync(_ => new PlayerHouse { RoomCount = rooms.Count + 1 });

        return (true, null, room);
    }

    // ═══════════════════════════════════════════
    //  Furniture
    // ═══════════════════════════════════════════

    public async Task<List<HouseFurniture>> GetFurnitureAsync(int houseId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<HouseFurniture>()
            .Where(x => x.HouseId == houseId)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<HouseFurniture>> GetRoomFurnitureAsync(int houseId, string roomName)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<HouseFurniture>()
            .Where(x => x.HouseId == houseId && x.RoomName == roomName)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool Success, string Error, FurnitureDef Item)> BuyFurnitureAsync(ulong userId, ulong guildId, string itemName)
    {
        var house = await GetHouseAsync(userId, guildId);
        if (house is null)
            return (false, "You don't own a house yet!", null);

        var key = (itemName ?? "").ToLowerInvariant();
        if (!_furnitureMap.TryGetValue(key, out var furnitureDef))
            return (false, "Item not found in the furniture shop. Use `.house shop` to see what's available.", null);

        var removed = await _cs.RemoveAsync(userId, furnitureDef.Price, new TxData("house", "furniture", $"Bought {furnitureDef.ItemName}"));
        if (!removed)
            return (false, $"You need **{furnitureDef.Price}** currency to buy **{furnitureDef.ItemName}**!", null);

        await using var ctx = _db.GetDbContext();
        var furniture = new HouseFurniture
        {
            HouseId = house.Id,
            RoomName = "", // not placed yet
            ItemName = furnitureDef.ItemName,
            ItemType = furnitureDef.ItemType,
            Rarity = furnitureDef.Rarity,
            BonusEffect = furnitureDef.BonusEffect,
        };
        ctx.Set<HouseFurniture>().Add(furniture);
        await ctx.SaveChangesAsync();

        return (true, null, furnitureDef);
    }

    public async Task<(bool Success, string Error)> PlaceFurnitureAsync(ulong userId, ulong guildId, string itemName, string roomName)
    {
        var house = await GetHouseAsync(userId, guildId);
        if (house is null)
            return (false, "You don't own a house yet!");

        var rooms = await GetRoomsAsync(house.Id);
        var targetRoom = rooms.FirstOrDefault(r => r.RoomName.Equals(roomName, StringComparison.OrdinalIgnoreCase));
        if (targetRoom is null)
            return (false, $"Room **{roomName}** not found. Use `.house myhouse` to see your rooms.");

        await using var ctx = _db.GetDbContext();

        // Find an unplaced furniture item with that name
        var furniture = await ctx.GetTable<HouseFurniture>()
            .FirstOrDefaultAsyncLinqToDB(x => x.HouseId == house.Id
                && x.ItemName.ToLower() == itemName.ToLower()
                && x.RoomName == "");

        if (furniture is null)
            return (false, $"You don't have an unplaced **{itemName}**. Buy one first with `.house buyfurniture`.");

        await ctx.GetTable<HouseFurniture>()
            .Where(x => x.Id == furniture.Id)
            .UpdateAsync(_ => new HouseFurniture { RoomName = targetRoom.RoomName });

        // Update furniture count on the room
        var roomFurnitureCount = await ctx.GetTable<HouseFurniture>()
            .Where(x => x.HouseId == house.Id && x.RoomName == targetRoom.RoomName)
            .CountAsyncLinqToDB();

        await ctx.GetTable<HouseRoom>()
            .Where(x => x.Id == targetRoom.Id)
            .UpdateAsync(_ => new HouseRoom { FurnitureCount = roomFurnitureCount });

        return (true, null);
    }

    // ═══════════════════════════════════════════
    //  Visiting & Guest Book
    // ═══════════════════════════════════════════

    public async Task<PlayerHouse> VisitHouseAsync(ulong visitorId, ulong targetUserId, ulong guildId)
    {
        var house = await GetHouseAsync(targetUserId, guildId);
        if (house is null)
            return null;

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<PlayerHouse>()
            .Where(x => x.Id == house.Id)
            .UpdateAsync(_ => new PlayerHouse { LastVisitedBy = visitorId });

        house.LastVisitedBy = visitorId;
        return house;
    }

    public async Task<(bool Success, string Error)> SignGuestBookAsync(int houseId, ulong visitorUserId, string visitorName, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return (false, "You need to write a message!");

        if (message.Length > 200)
            return (false, "Guest book messages can be at most 200 characters.");

        await using var ctx = _db.GetDbContext();

        // Check for duplicate recent entry (within 1 hour)
        var recent = await ctx.GetTable<GuestBookEntry>()
            .FirstOrDefaultAsyncLinqToDB(x => x.HouseId == houseId
                && x.VisitorUserId == visitorUserId
                && x.VisitedAt > DateTime.UtcNow.AddHours(-1));

        if (recent is not null)
            return (false, "You already signed this guest book recently! Wait an hour before signing again.");

        var entry = new GuestBookEntry
        {
            HouseId = houseId,
            VisitorUserId = visitorUserId,
            VisitorName = visitorName,
            Message = message,
            VisitedAt = DateTime.UtcNow,
        };
        ctx.Set<GuestBookEntry>().Add(entry);
        await ctx.SaveChangesAsync();

        // Increment guest book count on house
        var house = await ctx.GetTable<PlayerHouse>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == houseId);
        if (house is not null)
        {
            await ctx.GetTable<PlayerHouse>()
                .Where(x => x.Id == houseId)
                .UpdateAsync(_ => new PlayerHouse { GuestBookEntries = house.GuestBookEntries + 1 });
        }

        return (true, null);
    }

    public async Task<List<GuestBookEntry>> GetGuestBookAsync(int houseId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<GuestBookEntry>()
            .Where(x => x.HouseId == houseId)
            .OrderByDescending(x => x.VisitedAt)
            .Take(count)
            .ToListAsyncLinqToDB();
    }

    // ═══════════════════════════════════════════
    //  Style
    // ═══════════════════════════════════════════

    public async Task<(bool Success, string Error)> SetStyleAsync(ulong userId, ulong guildId, string style)
    {
        var house = await GetHouseAsync(userId, guildId);
        if (house is null)
            return (false, "You don't own a house yet!");

        if (!_validStyles.Contains(style))
            return (false, $"Invalid style. Choose from: {string.Join(", ", HouseStyles)}");

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<PlayerHouse>()
            .Where(x => x.Id == house.Id)
            .UpdateAsync(_ => new PlayerHouse { Style = style });

        return (true, null);
    }

    // ═══════════════════════════════════════════
    //  House Value Calculator
    // ═══════════════════════════════════════════

    public async Task<long> CalculateHouseValueAsync(ulong userId, ulong guildId)
    {
        var house = await GetHouseAsync(userId, guildId);
        if (house is null)
            return 0;

        long value = house.PurchasePrice;

        // Add room costs
        var rooms = await GetRoomsAsync(house.Id);
        foreach (var room in rooms)
        {
            var roomDef = _roomMap.GetValueOrDefault(room.RoomType.ToLowerInvariant());
            if (roomDef is not null)
                value += roomDef.UnlockCost;
        }

        // Add furniture values
        var furniture = await GetFurnitureAsync(house.Id);
        foreach (var item in furniture)
        {
            var furnitureDef = _furnitureMap.GetValueOrDefault(item.ItemName.ToLowerInvariant());
            if (furnitureDef is not null)
                value += furnitureDef.Price;
        }

        // Bonus value from trophy furniture
        foreach (var item in furniture)
        {
            if (item.BonusEffect.Contains("house value", StringComparison.OrdinalIgnoreCase))
            {
                // Parse "+50 house value" style bonuses
                var parts = item.BonusEffect.Split(' ');
                if (parts.Length > 0 && long.TryParse(parts[0].TrimStart('+'), out var bonus))
                    value += bonus;
            }
        }

        return value;
    }

    // ═══════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════

    public static string GetSizeEmoji(string size) => size?.ToLowerInvariant() switch
    {
        "cottage"  => "\ud83c\udfe1",
        "cabin"    => "\ud83c\udfd8\ufe0f",
        "house"    => "\ud83c\udfe0",
        "manor"    => "\ud83c\udfda\ufe0f",
        "mansion"  => "\ud83c\udfdb\ufe0f",
        "castle"   => "\ud83c\udff0",
        _          => "\ud83c\udfe0",
    };

    public static string GetRarityEmoji(string rarity) => rarity?.ToLowerInvariant() switch
    {
        "common"    => "\u26aa",
        "uncommon"  => "\ud83d\udfe2",
        "rare"      => "\ud83d\udd35",
        "epic"      => "\ud83d\udfe3",
        "legendary" => "\ud83d\udfe1",
        _           => "\u26aa",
    };

    public static string GetStyleEmoji(string style) => style?.ToLowerInvariant() switch
    {
        "medieval"   => "\u2694\ufe0f",
        "modern"     => "\ud83c\udfd9\ufe0f",
        "japanese"   => "\u26e9\ufe0f",
        "victorian"  => "\ud83c\udf39",
        "rustic"     => "\ud83e\udeb5",
        "fantasy"    => "\u2728",
        "steampunk"  => "\u2699\ufe0f",
        "underwater" => "\ud83c\udf0a",
        "treehouse"  => "\ud83c\udf33",
        "cloud"      => "\u2601\ufe0f",
        _            => "\ud83c\udfe0",
    };
}
