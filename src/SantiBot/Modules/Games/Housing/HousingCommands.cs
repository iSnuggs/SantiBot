#nullable disable
using SantiBot.Modules.Games.Housing;
using System.Text;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Housing")]
    [Group("house")]
    public partial class HousingCommands : SantiModule<HousingService>
    {
        // ─── Buy a House ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Buy([Leftover] string size = "Cottage")
        {
            var (success, error, house) = await _service.BuyHouseAsync(ctx.User.Id, ctx.Guild.Id, size);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var emoji = HousingService.GetSizeEmoji(house.HouseSize);
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{emoji} House Purchased!")
                .WithDescription(
                    $"You are now the proud owner of a **{house.HouseSize}**!\n\n" +
                    $"**Style:** {house.Style}\n" +
                    $"**Rooms:** {house.RoomCount}\n" +
                    $"**Trophy Slots:** {house.TrophySlots}\n" +
                    $"**Garden Size:** {house.GardenSize}\n\n" +
                    $"Use `.house addroom` to add rooms and `.house shop` to browse furniture!")
                .WithFooter($"Cost: {house.PurchasePrice} currency");

            await Response().Embed(eb).SendAsync();
        }

        // ─── Upgrade House ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Upgrade()
        {
            var (success, error, house, oldSize, newSize) = await _service.UpgradeHouseAsync(ctx.User.Id, ctx.Guild.Id);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var emoji = HousingService.GetSizeEmoji(newSize);
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{emoji} House Upgraded!")
                .WithDescription(
                    $"Your **{oldSize}** has been upgraded to a **{newSize}**!\n\n" +
                    $"**Level:** {house.Level}\n" +
                    $"**Trophy Slots:** {house.TrophySlots}\n" +
                    $"**Garden Size:** {house.GardenSize}\n\n" +
                    $"More room for activities!");

            await Response().Embed(eb).SendAsync();
        }

        // ─── Add Room ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AddRoom(string roomType, [Leftover] string roomName = null)
        {
            var (success, error, room) = await _service.AddRoomAsync(ctx.User.Id, ctx.Guild.Id, roomType, roomName);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Room Added!")
                .WithDescription(
                    $"You added a new **{room.RoomType}** room: **{room.RoomName}**\n\n" +
                    $"Furnish it with `.house buyfurniture` and `.house decorate`!");

            await Response().Embed(eb).SendAsync();
        }

        // ─── Buy Furniture ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BuyFurniture([Leftover] string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                await Response().Error("Specify a furniture item to buy. Use `.house shop` to see what's available.").SendAsync();
                return;
            }

            var (success, error, item) = await _service.BuyFurnitureAsync(ctx.User.Id, ctx.Guild.Id, itemName);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var rarityEmoji = HousingService.GetRarityEmoji(item.Rarity);
            var sb = new StringBuilder();
            sb.AppendLine($"You purchased **{item.ItemName}** {rarityEmoji} ({item.Rarity})");
            sb.AppendLine($"**Type:** {item.ItemType}");
            if (!string.IsNullOrEmpty(item.BonusEffect))
                sb.AppendLine($"**Bonus:** {item.BonusEffect}");
            sb.AppendLine();
            sb.AppendLine("Place it in a room with `.house decorate <item> <room>`");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Furniture Purchased!")
                .WithDescription(sb.ToString())
                .WithFooter($"Cost: {item.Price} currency");

            await Response().Embed(eb).SendAsync();
        }

        // ─── Decorate (Place Furniture) ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Decorate(string itemName, [Leftover] string roomName)
        {
            if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(roomName))
            {
                await Response().Error("Usage: `.house decorate <item name> <room name>`").SendAsync();
                return;
            }

            var (success, error) = await _service.PlaceFurnitureAsync(ctx.User.Id, ctx.Guild.Id, itemName, roomName);

            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"Placed **{itemName}** in **{roomName}**!").SendAsync();
        }

        // ─── My House ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task MyHouse()
        {
            var house = await _service.GetHouseAsync(ctx.User.Id, ctx.Guild.Id);
            if (house is null)
            {
                await Response().Error("You don't own a house yet! Use `.house buy` to purchase one.").SendAsync();
                return;
            }

            var rooms = await _service.GetRoomsAsync(house.Id);
            var furniture = await _service.GetFurnitureAsync(house.Id);
            var value = await _service.CalculateHouseValueAsync(ctx.User.Id, ctx.Guild.Id);

            var sizeEmoji = HousingService.GetSizeEmoji(house.HouseSize);
            var styleEmoji = HousingService.GetStyleEmoji(house.Style);

            var sb = new StringBuilder();
            sb.AppendLine($"**Size:** {sizeEmoji} {house.HouseSize} (Level {house.Level})");
            sb.AppendLine($"**Style:** {styleEmoji} {house.Style}");
            sb.AppendLine($"**Rooms:** {rooms.Count}");
            sb.AppendLine($"**Trophy Slots:** {house.TrophySlots}");
            sb.AppendLine($"**Garden Size:** {house.GardenSize}");
            sb.AppendLine($"**Guest Book Entries:** {house.GuestBookEntries}");
            sb.AppendLine($"**Total Value:** {value:N0} currency");
            sb.AppendLine();

            // List rooms
            if (rooms.Count > 0)
            {
                sb.AppendLine("**Rooms:**");
                foreach (var room in rooms)
                {
                    var roomFurniture = furniture.Where(f => f.RoomName == room.RoomName).ToList();
                    sb.AppendLine($"  - **{room.RoomName}** ({room.RoomType}) - {roomFurniture.Count} item(s)");
                }
                sb.AppendLine();
            }

            // List unplaced furniture
            var unplaced = furniture.Where(f => string.IsNullOrEmpty(f.RoomName)).ToList();
            if (unplaced.Count > 0)
            {
                sb.AppendLine("**Unplaced Furniture:**");
                foreach (var item in unplaced)
                {
                    var re = HousingService.GetRarityEmoji(item.Rarity);
                    sb.AppendLine($"  - {re} {item.ItemName} ({item.Rarity})");
                }
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{sizeEmoji} {house.HouseName} - {ctx.User.Username}'s Home")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        // ─── Visit Another User's House ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Visit([Leftover] IUser user)
        {
            if (user is null)
            {
                await Response().Error("Mention a user whose house you want to visit.").SendAsync();
                return;
            }

            if (user.Id == ctx.User.Id)
            {
                await Response().Error("Use `.house myhouse` to view your own house!").SendAsync();
                return;
            }

            var house = await _service.VisitHouseAsync(ctx.User.Id, user.Id, ctx.Guild.Id);
            if (house is null)
            {
                await Response().Error($"**{user.Username}** doesn't own a house yet.").SendAsync();
                return;
            }

            var rooms = await _service.GetRoomsAsync(house.Id);
            var furniture = await _service.GetFurnitureAsync(house.Id);
            var value = await _service.CalculateHouseValueAsync(user.Id, ctx.Guild.Id);

            var sizeEmoji = HousingService.GetSizeEmoji(house.HouseSize);
            var styleEmoji = HousingService.GetStyleEmoji(house.Style);

            var sb = new StringBuilder();
            sb.AppendLine($"**Size:** {sizeEmoji} {house.HouseSize} (Level {house.Level})");
            sb.AppendLine($"**Style:** {styleEmoji} {house.Style}");
            sb.AppendLine($"**Rooms:** {rooms.Count}");
            sb.AppendLine($"**Guest Book Entries:** {house.GuestBookEntries}");
            sb.AppendLine($"**Total Value:** {value:N0} currency");
            sb.AppendLine();

            if (rooms.Count > 0)
            {
                sb.AppendLine("**Rooms:**");
                foreach (var room in rooms)
                {
                    var roomFurniture = furniture.Where(f => f.RoomName == room.RoomName).ToList();
                    sb.AppendLine($"  - **{room.RoomName}** ({room.RoomType}) - {roomFurniture.Count} item(s)");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Sign their guest book with `.house guestbook sign <message>`!");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{sizeEmoji} {house.HouseName} - {user.Username}'s Home")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        // ─── Guest Book ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GuestBook([Leftover] string args = null)
        {
            // Parse: "sign <message>" or "read @user" or just "read" (own book)
            if (!string.IsNullOrWhiteSpace(args) && args.StartsWith("sign ", StringComparison.OrdinalIgnoreCase))
            {
                // Sign the last visited house
                var message = args[5..].Trim();
                var visitorHouse = await _service.GetHouseAsync(ctx.User.Id, ctx.Guild.Id);

                // We need a house to sign - find via last visited
                // For simplicity, let the user specify who to sign for by visiting first
                // Check if they have their own house for the houseId context
                if (visitorHouse is null)
                {
                    await Response().Error("You need to own a house to sign guest books. Use `.house buy` first!").SendAsync();
                    return;
                }

                // They sign their OWN guest book? No - they sign the house they last visited
                // We'll look up who they last visited via mentions in the channel
                // Simplified: sign your own guest book is disabled
                await Response().Error("Visit someone's house first with `.house visit @user`, then use `.house guestbook sign <message>` while in their house.\n\nTo read your own guest book, just use `.house guestbook`.").SendAsync();
                return;
            }

            // Read own guest book
            var house = await _service.GetHouseAsync(ctx.User.Id, ctx.Guild.Id);
            if (house is null)
            {
                await Response().Error("You don't own a house yet!").SendAsync();
                return;
            }

            var entries = await _service.GetGuestBookAsync(house.Id);

            if (entries.Count == 0)
            {
                await Response().Confirm("Your guest book is empty. Invite friends to visit with `.house visit`!").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var entry in entries)
            {
                var timeAgo = (DateTime.UtcNow - entry.VisitedAt).TotalHours;
                var timeStr = timeAgo < 1 ? "just now"
                    : timeAgo < 24 ? $"{(int)timeAgo}h ago"
                    : $"{(int)(timeAgo / 24)}d ago";
                sb.AppendLine($"**{entry.VisitorName}** ({timeStr}):");
                sb.AppendLine($"  *\"{entry.Message}\"*");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Guest Book - {ctx.User.Username}'s Home")
                .WithDescription(sb.ToString())
                .WithFooter($"{entries.Count} recent entries");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GuestBookSign(IUser targetUser, [Leftover] string message)
        {
            if (targetUser is null || string.IsNullOrWhiteSpace(message))
            {
                await Response().Error("Usage: `.house guestbooksign @user <message>`").SendAsync();
                return;
            }

            var house = await _service.GetHouseAsync(targetUser.Id, ctx.Guild.Id);
            if (house is null)
            {
                await Response().Error($"**{targetUser.Username}** doesn't own a house.").SendAsync();
                return;
            }

            if (targetUser.Id == ctx.User.Id)
            {
                await Response().Error("You can't sign your own guest book!").SendAsync();
                return;
            }

            var (success, error) = await _service.SignGuestBookAsync(house.Id, ctx.User.Id, ctx.User.Username, message);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"You signed **{targetUser.Username}**'s guest book!").SendAsync();
        }

        // ─── Styles ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Styles()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Choose a style for your home:\n");
            foreach (var style in HousingService.HouseStyles)
            {
                var emoji = HousingService.GetStyleEmoji(style);
                sb.AppendLine($"  {emoji} **{style}**");
            }
            sb.AppendLine();
            sb.AppendLine("Set your style with `.house setstyle <name>`");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("House Styles")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SetStyle([Leftover] string style)
        {
            if (string.IsNullOrWhiteSpace(style))
            {
                await Response().Error("Specify a style. Use `.house styles` to see options.").SendAsync();
                return;
            }

            // Capitalize first letter for matching
            style = char.ToUpper(style[0]) + style[1..].ToLower();

            var (success, error) = await _service.SetStyleAsync(ctx.User.Id, ctx.Guild.Id, style);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var emoji = HousingService.GetStyleEmoji(style);
            await Response().Confirm($"Your house style is now {emoji} **{style}**!").SendAsync();
        }

        // ─── House Shop ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task HouseShop([Leftover] string category = null)
        {
            var sb = new StringBuilder();

            if (string.IsNullOrWhiteSpace(category))
            {
                // Show house sizes & overview
                sb.AppendLine("**Houses:**");
                foreach (var size in HousingService.HouseSizes)
                {
                    var emoji = HousingService.GetSizeEmoji(size.Name);
                    sb.AppendLine($"  {emoji} **{size.Name}** - {size.Price:N0} currency ({size.MaxRooms} rooms)");
                }
                sb.AppendLine();
                sb.AppendLine("**Room Types:**");
                foreach (var room in HousingService.RoomTypes)
                {
                    sb.AppendLine($"  **{room.Name}** - {room.UnlockCost:N0} currency");
                }
                sb.AppendLine();
                sb.AppendLine("View furniture by category:");
                sb.AppendLine("`.house houseshop Table` / `Chair` / `Bed` / `Shelf` / `Lamp` / `Rug` / `Painting` / `Trophy` / `Plant` / `Aquarium` / `Instrument`");

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle("House Shop")
                    .WithDescription(sb.ToString());

                await Response().Embed(eb).SendAsync();
                return;
            }

            // Show furniture by category
            var matchedItems = HousingService.FurnitureCatalog
                .Where(f => f.ItemType.Equals(category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Price)
                .ToList();

            if (matchedItems.Count == 0)
            {
                await Response().Error($"No furniture found for category **{category}**. Try: Table, Chair, Bed, Shelf, Lamp, Rug, Painting, Trophy, Plant, Aquarium, Instrument").SendAsync();
                return;
            }

            sb.AppendLine($"**{category} Furniture:**\n");
            foreach (var item in matchedItems)
            {
                var re = HousingService.GetRarityEmoji(item.Rarity);
                sb.Append($"  {re} **{item.ItemName}** ({item.Rarity}) - {item.Price:N0} currency");
                if (!string.IsNullOrEmpty(item.BonusEffect))
                    sb.Append($" | *{item.BonusEffect}*");
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine("Buy with `.house buyfurniture <name>`");

            var embed = CreateEmbed()
                .WithOkColor()
                .WithTitle($"House Shop - {category}")
                .WithDescription(sb.ToString());

            await Response().Embed(embed).SendAsync();
        }

        // ─── House Value ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task HouseValue([Leftover] IUser user = null)
        {
            var targetUser = user ?? ctx.User;
            var value = await _service.CalculateHouseValueAsync(targetUser.Id, ctx.Guild.Id);

            if (value == 0)
            {
                var msg = targetUser.Id == ctx.User.Id
                    ? "You don't own a house yet!"
                    : $"**{targetUser.Username}** doesn't own a house.";
                await Response().Error(msg).SendAsync();
                return;
            }

            var house = await _service.GetHouseAsync(targetUser.Id, ctx.Guild.Id);
            var furniture = await _service.GetFurnitureAsync(house.Id);
            var rooms = await _service.GetRoomsAsync(house.Id);

            var furnitureValue = furniture.Sum(f =>
            {
                var def = HousingService.FurnitureCatalog
                    .FirstOrDefault(c => c.ItemName.Equals(f.ItemName, StringComparison.OrdinalIgnoreCase));
                return def?.Price ?? 0;
            });

            var roomValue = rooms.Sum(r =>
            {
                var def = HousingService.RoomTypes
                    .FirstOrDefault(rt => rt.Name.Equals(r.RoomType, StringComparison.OrdinalIgnoreCase));
                return def?.UnlockCost ?? 0;
            });

            var emoji = HousingService.GetSizeEmoji(house.HouseSize);
            var sb = new StringBuilder();
            sb.AppendLine($"**House:** {emoji} {house.HouseSize} - {house.PurchasePrice:N0}");
            sb.AppendLine($"**Rooms ({rooms.Count}):** {roomValue:N0}");
            sb.AppendLine($"**Furniture ({furniture.Count}):** {furnitureValue:N0}");
            sb.AppendLine();
            sb.AppendLine($"**Total Value: {value:N0} currency**");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"House Appraisal - {targetUser.Username}")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
