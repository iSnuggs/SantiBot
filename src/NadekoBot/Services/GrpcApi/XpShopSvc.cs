using Grpc.Core;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Xp;
using NadekoBot.Modules.Xp.Services;

namespace NadekoBot.GrpcApi;

public class XpShopSvc(XpService xp, XpConfigService xpConfig) : GrpcXpShop.GrpcXpShopBase, IGrpcSvc, INService
{
    public ServerServiceDefinition Bind()
        => GrpcXpShop.BindService(this);

    [GrpcNoAuthRequired]
    public override async Task<BuyShopItemReply> BuyShopItem(BuyShopItemRequest request, ServerCallContext context)
    {
        var result = await xp.BuyShopItemAsync(request.UserId, (XpShopItemType)request.ItemType, request.UniqueName);

        var res = new BuyShopItemReply();

        if (result == BuyResult.Success)
        {
            res.Success = true;
            return res;
        }

        res.Error = result switch
        {
            BuyResult.AlreadyOwned => BuyShopItemError.AlreadyOwned,
            BuyResult.InsufficientFunds => BuyShopItemError.NotEnough,
            _ => BuyShopItemError.Unknown
        };

        return res;
    }

    [GrpcNoAuthRequired]
    public override async Task<UseShopItemReply> UseShopItem(UseShopItemRequest request, ServerCallContext context)
    {
        var result = await xp.UseShopItemAsync(request.UserId, (XpShopItemType)request.ItemType, request.UniqueName);

        var res = new UseShopItemReply
        {
            Success = result
        };

        return res;
    }

    [GrpcNoAuthRequired]
    public override async Task<GetShopItemsReply> GetShopItems(GetShopItemsRequest request, ServerCallContext context)
    {
        var userItems = await xp.GetUserItemsAsync(request.UserId);
        try{
        var items = await xp.GetShopItems();
        var res = new GetShopItemsReply();
        res.Bgs.AddRange(items.Where(x => x.ItemType == XpShopItemType.Bg).Select(MapItemToGrpcItem));
        res.Frames.AddRange(items.Where(x => x.ItemType == XpShopItemType.Frame).Select(MapItemToGrpcItem));
        

        return res;
        }
        catch(Exception e){
            Log.Error(e, "Error getting shop items");
            throw;
            }

        XpShopItem MapItemToGrpcItem(XpConfig.ShopItemInfo item)
        {
            return new XpShopItem()
            {
                Name = item.Name,
                Price = item.Price,
                Description = item.Desc ?? "",
                FullUrl = item.Url ?? "",
                PreviewUrl = item.Preview ?? "",
                ItemType = (GrpcXpShopItemType)item.ItemType,
                UniqueName = item.UniqueName,
                Owned = userItems.Contains((item.ItemType, item.UniqueName))
            };
        }
    }

    [GrpcNoAuthRequired]
    public override async Task<AddXpShopItemReply> AddXpShopItem(AddXpShopItemRequest request,
        ServerCallContext context)
    {
        var result = await xpConfig.AddItemAsync(
            new XpConfig.ShopItemInfo()
            {
                Name = request.Item.Name,
                Price = 3000,
                Desc = request.Item.Description,
                Url = request.Item.FullUrl,
                Preview = request.Item.PreviewUrl,
                UniqueName = request.Item.UniqueName,
                ItemType = (XpShopItemType)request.Item.ItemType,
            });

        return new AddXpShopItemReply()
        {
            Success = result,
        };
    }
}