#nullable disable
using NadekoBot.Common.Configs;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Xp.Services;

public sealed class XpConfigService : ConfigServiceBase<XpConfig>
{
    private const string FILE_PATH = "data/xp.yml";
    private static readonly TypedKey<XpConfig> _changeKey = new("config.xp.updated");

    public override string Name
        => "xp";

    public XpConfigService(IConfigSeria serializer, IPubSub pubSub)
        : base(FILE_PATH, serializer, pubSub, _changeKey)
    {
        AddParsedProp("txt.cooldown",
            conf => conf.TextXpCooldown,
            int.TryParse,
            (f) => f.ToString("F2"),
            x => x > 0);

        AddParsedProp("txt.permsg",
            conf => conf.TextXpPerMessage,
            int.TryParse,
            ConfigPrinters.ToString,
            x => x >= 0);

        AddParsedProp("txt.perimage",
            conf => conf.TextXpFromImage,
            int.TryParse,
            ConfigPrinters.ToString,
            x => x > 0);

        AddParsedProp("voice.perminute",
            conf => conf.VoiceXpPerMinute,
            int.TryParse,
            ConfigPrinters.ToString,
            x => x >= 0);

        AddParsedProp("shop.is_enabled",
            conf => conf.Shop.IsEnabled,
            bool.TryParse,
            ConfigPrinters.ToString);

        Migrate();
    }

    private void Migrate()
    {
        if (data.Version < 11)
        {
            ModifyConfig(c =>
            {
                c.Version = 12;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (c.Shop is null)
                    c.Shop = new();

                var shop = c.Shop;
                shop.Items = [];

#pragma warning disable CS0618 // Type or member is obsolete
                if (shop.Bgs is not null)
                {
                    foreach (var (k, item) in shop.Bgs)
                    {
                        if (k == "default" || string.IsNullOrWhiteSpace(item.Url))
                            continue;

                        item.ItemType = XpShopItemType.Bg;
                        item.UniqueName = k;
                        shop.Items.Add(item);
                    }
                }

                if (shop.Frames is not null)
                {
                    foreach (var (k, item) in shop.Frames)
                    {
                        if (k == "default" || string.IsNullOrWhiteSpace(item.Url))
                            continue;

                        item.ItemType = XpShopItemType.Frame;
                        item.UniqueName = k;
                        shop.Items.Add(item);
                    }
                }
#pragma warning restore CS0618 // Type or member is obsolete
            });
        }
    }

    public async Task<bool> AddItemAsync(XpConfig.ShopItemInfo shopItemInfo)
    {
        await Task.Yield();

        var success = false;
        ModifyConfig(c =>
        {
            var items = c.Shop.Items;


            if (items.Any(x => x.UniqueName == shopItemInfo.UniqueName))
                return;

            items.Add(shopItemInfo);
            success = true;
        });

        return success;
    }
}