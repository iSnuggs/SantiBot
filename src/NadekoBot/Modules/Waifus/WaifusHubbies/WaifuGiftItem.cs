namespace NadekoBot.Modules.Waifus.WaifusHubbies;

/// <summary>
/// Type of gift item effect.
/// </summary>
public enum GiftItemType
{
    /// <summary>
    /// Increases Food stat.
    /// </summary>
    Food,

    /// <summary>
    /// Increases Mood stat.
    /// </summary>
    Mood
}

/// <summary>
/// Represents a gift item that can be given to a waifu.
/// </summary>
/// <param name="Id">Unique identifier for the item.</param>
/// <param name="Emoji">Emoji representation of the item.</param>
/// <param name="Name">Display name of the item.</param>
/// <param name="Price">Cost in currency to purchase.</param>
/// <param name="Type">Whether this item affects Food or Mood.</param>
/// <param name="Effect">Amount to increase the stat by.</param>
public sealed record WaifuGiftItem(
    Guid Id,
    string Emoji,
    string Name,
    long Price,
    GiftItemType Type,
    int Effect
);

/// <summary>
/// Provides access to gift items and daily rotation logic.
/// </summary>
public static class WaifuGiftItems
{
    private static readonly DateTime Epoch = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly long[] PriceTiers = [10, 50, 250, 1000, 2500, 5000];

    /// <summary>
    /// All available gift items (24 total: 12 food, 12 mood).
    /// </summary>
    public static IReadOnlyList<WaifuGiftItem> AllItems { get; } =
    [
        // Food items - Price 10
        new(Guid.Parse("019479a1-0001-7000-8000-000000000001"), "🍪", "Cookie", 10, GiftItemType.Food, 5),
        new(Guid.Parse("019479a1-0002-7000-8000-000000000002"), "🍩", "Donut", 10, GiftItemType.Food, 5),
        // Food items - Price 50
        new(Guid.Parse("019479a1-0003-7000-8000-000000000003"), "🥖", "Bread", 50, GiftItemType.Food, 15),
        new(Guid.Parse("019479a1-0004-7000-8000-000000000004"), "🍙", "Onigiri", 50, GiftItemType.Food, 15),
        // Food items - Price 250
        new(Guid.Parse("019479a1-0005-7000-8000-000000000005"), "🍕", "Pizza", 250, GiftItemType.Food, 40),
        new(Guid.Parse("019479a1-0006-7000-8000-000000000006"), "🍔", "Burger", 250, GiftItemType.Food, 40),
        // Food items - Price 1000
        new(Guid.Parse("019479a1-0007-7000-8000-000000000007"), "🍱", "Bento", 1000, GiftItemType.Food, 80),
        new(Guid.Parse("019479a1-0008-7000-8000-000000000008"), "🍝", "Pasta", 1000, GiftItemType.Food, 80),
        // Food items - Price 2500
        new(Guid.Parse("019479a1-0009-7000-8000-000000000009"), "🍰", "Cake", 2500, GiftItemType.Food, 150),
        new(Guid.Parse("019479a1-000a-7000-8000-000000000010"), "🍣", "Sushi", 2500, GiftItemType.Food, 150),
        // Food items - Price 5000
        new(Guid.Parse("019479a1-000b-7000-8000-000000000011"), "🦞", "Lobster", 5000, GiftItemType.Food, 250),
        new(Guid.Parse("019479a1-000c-7000-8000-000000000012"), "🍖", "Feast", 5000, GiftItemType.Food, 250),

        // Mood items - Price 10
        new(Guid.Parse("019479a1-1001-7000-8000-000000000101"), "🌸", "Flower", 10, GiftItemType.Mood, 5),
        new(Guid.Parse("019479a1-1002-7000-8000-000000000102"), "🎀", "Ribbon", 10, GiftItemType.Mood, 5),
        // Mood items - Price 50
        new(Guid.Parse("019479a1-1003-7000-8000-000000000103"), "🌹", "Rose", 50, GiftItemType.Mood, 15),
        new(Guid.Parse("019479a1-1004-7000-8000-000000000104"), "💌", "LoveLetter", 50, GiftItemType.Mood, 15),
        // Mood items - Price 250
        new(Guid.Parse("019479a1-1005-7000-8000-000000000105"), "🧸", "Teddy", 250, GiftItemType.Mood, 40),
        new(Guid.Parse("019479a1-1006-7000-8000-000000000106"), "🎁", "Gift", 250, GiftItemType.Mood, 40),
        // Mood items - Price 1000
        new(Guid.Parse("019479a1-1007-7000-8000-000000000107"), "💎", "Diamond", 1000, GiftItemType.Mood, 80),
        new(Guid.Parse("019479a1-1008-7000-8000-000000000108"), "👗", "Dress", 1000, GiftItemType.Mood, 80),
        // Mood items - Price 2500
        new(Guid.Parse("019479a1-1009-7000-8000-000000000109"), "🎹", "Piano", 2500, GiftItemType.Mood, 150),
        new(Guid.Parse("019479a1-100a-7000-8000-000000000110"), "🐱", "Kitten", 2500, GiftItemType.Mood, 150),
        // Mood items - Price 5000
        new(Guid.Parse("019479a1-100b-7000-8000-000000000111"), "🏠", "House", 5000, GiftItemType.Mood, 250),
        new(Guid.Parse("019479a1-100c-7000-8000-000000000112"), "🌙", "Moon", 5000, GiftItemType.Mood, 250),
    ];

    /// <summary>
    /// Gets the 6 items available today (3 food, 3 mood - one per price tier).
    /// </summary>
    public static IReadOnlyList<WaifuGiftItem> GetTodaysItems()
    {
        var daysSinceEpoch = (int)(DateTime.UtcNow.Date - Epoch).TotalDays;
        var result = new List<WaifuGiftItem>(6);

        for (var tierIndex = 0; tierIndex < PriceTiers.Length; tierIndex++)
        {
            var price = PriceTiers[tierIndex];

            // Get all items at this price tier
            var foodItems = AllItems.Where(x => x.Price == price && x.Type == GiftItemType.Food).ToList();
            var moodItems = AllItems.Where(x => x.Price == price && x.Type == GiftItemType.Mood).ToList();

            // Alternate between food and mood based on tier index + day
            // This ensures 3 food and 3 mood items each day
            var isFood = (tierIndex + daysSinceEpoch) % 2 == 0;

            if (isFood)
            {
                // Pick which food item variant based on day
                var variant = (daysSinceEpoch / 6) % foodItems.Count;
                result.Add(foodItems[variant]);
            }
            else
            {
                // Pick which mood item variant based on day
                var variant = (daysSinceEpoch / 6) % moodItems.Count;
                result.Add(moodItems[variant]);
            }
        }

        return result.OrderBy(x => x.Price).ToList();
    }

    /// <summary>
    /// Gets the time remaining until the shop refreshes (next 00:00 UTC).
    /// </summary>
    public static TimeSpan GetTimeUntilRefresh()
    {
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        return nextMidnight - now;
    }

    /// <summary>
    /// Finds an item by name from today's available items.
    /// </summary>
    public static WaifuGiftItem? FindTodaysItem(string name)
    {
        var todaysItems = GetTodaysItems();
        return todaysItems.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds an item by ID from today's available items.
    /// </summary>
    public static WaifuGiftItem? FindTodaysItemById(Guid id)
    {
        var todaysItems = GetTodaysItems();
        return todaysItems.FirstOrDefault(x => x.Id == id);
    }
}
