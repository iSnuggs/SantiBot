using System.Text;
using NadekoBot.Modules.Gambling.Bank;
using NadekoBot.Modules.Waifus.Waifu;

namespace NadekoBot.Modules.Waifus;

public partial class Waifus
{
    public class WaifuCommands(WaifuService svc, ICurrencyProvider cp, IBankService bank) : NadekoModule
    {
    private static string BuildProgressBar(double fraction, int length, char filled = '█', char empty = '░')
    {
        var filledCount = (int)Math.Round(fraction * length);
        filledCount = Math.Clamp(filledCount, 0, length);
        return new string(filled, filledCount) + new string(empty, length - filledCount);
    }

    private static Discord.Color GetConditionColor(int conditionPercent)
        => conditionPercent switch
        {
            > 60 => new Discord.Color(0x2E, 0xCC, 0x71), // green
            > 30 => new Discord.Color(0xF3, 0x9C, 0x12), // amber
            _ => new Discord.Color(0xE7, 0x4C, 0x3C)      // red
        };

    private NadekoInteractionBase CreateWaifuCardInteraction()
        => _inter.Create(ctx.User.Id,
            new ButtonBuilder(
                customId: "woptin:show_card",
                emote: new Emoji("👤")),
            async smc =>
            {
                await ShowWaifuCardAsync(ctx.User.Id);
                await smc.DeferAsync();
            });

    private NadekoInteractionBase CreateBankInteraction()
        => _inter.Create(ctx.User.Id,
            new ButtonBuilder(
                customId: "wback:bank_balance",
                emote: new Emoji("🏦")),
            BankAction);

    private NadekoInteractionBase CreateOptInInteraction()
        => _inter.Create(ctx.User.Id,
            new ButtonBuilder(
                label: "Opt In",
                customId: "waifu:opt_in",
                emote: new Emoji("🎀"),
                style: ButtonStyle.Success),
            async smc =>
            {
                var res = await svc.OptInAsync(ctx.User.Id);
                await res.Match(
                    _ => smc.RespondConfirmAsync(_sender, "You're already a waifu!"),
                    _ => smc.RespondAsync(_sender, "Not enough funds!", MsgType.Error),
                    _ => smc.RespondConfirmAsync(_sender, $"You are now a waifu! Use `{prefix}waifu` to see your card."));
            });

    private NadekoInteractionBase CreateCollectPayoutInteraction()
        => _inter.Create(ctx.User.Id,
            new ButtonBuilder(
                label: "Collect",
                customId: "waifu:collect_payout",
                emote: new Emoji("💰"),
                style: ButtonStyle.Success),
            async smc =>
            {
                var result = await svc.ClaimPayoutAsync(ctx.User.Id);
                var currSign = cp.GetCurrencySign();
                await result.Match(
                    _ => smc.RespondAsync(_sender, GetText(strs.waifu_no_pending_payout), MsgType.Error, ephemeral: true),
                    claimed => smc.RespondConfirmAsync(_sender,
                        GetText(strs.waifu_payout_claimed(CurrencyHelper.N(claimed.Value, Culture, currSign))),
                        ephemeral: true));
            });

    private async Task BankAction(SocketMessageComponent smc)
    {
        var balance = await bank.GetBalanceAsync(ctx.User.Id);
        var currSign = cp.GetCurrencySign();
        await smc.RespondConfirmAsync(_sender,
            GetText(strs.waifu_bank_balance(CurrencyHelper.N(balance, Culture, currSign))),
            ephemeral: true);
    }

    private async Task ShowWaifuCardAsync(ulong targetUserId, string? banner = null)
    {
        var info = await svc.GetWaifuInfoAsync(targetUserId);
        if (info is null)
        {
            await Response().Error(strs.waifu_not_opted_in).SendAsync();
            return;
        }

        var targetUser = await ctx.Client.GetUserAsync(targetUserId);

        var projection = await svc.GetProjectedPayoutAsync(targetUserId);
        var manager = info.ManagerId.HasValue
            ? await ctx.Client.GetUserAsync(info.ManagerId.Value)
            : null;

        var moodPercent = info.Mood / 10;
        var foodPercent = info.Food / 10;
        var condition = (moodPercent + foodPercent) / 2;
        var cycleProgress = svc.GetCycleProgressFraction();
        var cyclePercent = (int)Math.Round(cycleProgress * 100);
        var payoutUnix = new DateTimeOffset(svc.GetNextCycleTime()).ToUnixTimeSeconds();
        var cycleNum = svc.GetCurrentCycle();
        var currSign = cp.GetCurrencySign();

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(banner))
            sb.AppendLine($"> {banner}");

        if (!string.IsNullOrWhiteSpace(info.Quote))
            sb.AppendLine($"*\"{info.Quote}\"*");

        if (!string.IsNullOrWhiteSpace(info.Description))
            sb.AppendLine(info.Description);

        if (sb.Length > 0)
            sb.AppendLine();

        sb.AppendLine($"😊 `{BuildProgressBar(moodPercent / 100.0, 10, '▰', '▱')}` {moodPercent}%");
        sb.AppendLine($"🍔 `{BuildProgressBar(foodPercent / 100.0, 10, '▰', '▱')}` {foodPercent}%");

        var statMultiplier = (info.Mood + info.Food) / 2000.0;
        var snapshotBacked = info.SnapshotTotalBacked;
        var overCap = snapshotBacked > info.ReturnsCap && snapshotBacked > 0;
        var capMultiplier = snapshotBacked > 0
            ? Math.Min(1.0, info.ReturnsCap / (double)snapshotBacked)
            : 1.0;
        var efficiency = (int)Math.Round(statMultiplier * capMultiplier * 100);
        var effBar = BuildProgressBar(efficiency / 100.0, 10, '▰', '▱');
        var efficiencyText = overCap
            ? $"⚡ `{effBar}` {efficiency}% ({GetText(strs.waifu_over_cap)})"
            : $"⚡ `{effBar}` {efficiency}%";
        sb.AppendLine(efficiencyText);
        sb.AppendLine();

        sb.AppendLine($"**Cycle #{cycleNum}**");
        sb.AppendLine($"`{BuildProgressBar(cycleProgress, 20)}` {cyclePercent}%");
        sb.AppendLine($"Payout <t:{payoutUnix}:R>");
        sb.AppendLine();

        if (info.ManagerId is null)
        {
            var decayPercent = svc.ManagerlessDecayPercent;
            sb.AppendLine($"⚠\uFE0F **{GetText(strs.waifu_no_manager_title)}**");
            sb.AppendLine(GetText(strs.waifu_no_manager_payout(decayPercent)));
        }
        else if (projection is not null && projection.TotalReturns > 0)
        {
            sb.AppendLine($"💰 **{GetText(strs.waifu_projected_payout)}**");
            sb.AppendLine($"  {GetText(strs.waifu_total)}: {CurrencyHelper.N(projection.TotalReturns, Culture, currSign)}");
            sb.AppendLine($"  ├ 🎀 {GetText(strs.waifu_label)}: {CurrencyHelper.N(projection.WaifuCut, Culture, currSign)}");
            sb.AppendLine($"  ├ 🏢 {GetText(strs.waifu_manager)}: {CurrencyHelper.N(projection.ManagerCut, Culture, currSign)}");
            sb.AppendLine($"  └ 👥 {GetText(strs.waifu_fan_pool)}: {CurrencyHelper.N(projection.FanPool, Culture, currSign)}");
        }

        var backedBySb = new StringBuilder();
        backedBySb.Append($"{info.FanCount} fans");
        if (snapshotBacked > 0)
            backedBySb.Append($" ({CurrencyHelper.N(snapshotBacked, Culture, currSign)})");
        if (info.PendingJoins > 0 || info.PendingLeaves > 0)
            backedBySb.Append($"\n{GetText(strs.waifu_pending_fans(info.PendingJoins, info.PendingLeaves))}");
        if (info.LastCycleReturns > 0)
            backedBySb.Append($"\n{GetText(strs.waifu_last_payout)}: {CurrencyHelper.N(info.LastCycleReturns, Culture, currSign)}");

        var displayName = targetUser?.ToString() ?? targetUserId.ToString();
        var eb = CreateEmbed()
            .WithColor(GetConditionColor(condition))
            .WithAuthor(displayName, targetUser?.GetAvatarUrl() ?? targetUser?.GetDefaultAvatarUrl())
            .WithDescription(sb.ToString())
            .AddField("👥 Backed By", backedBySb.ToString(), true)
            .AddField("🏢 Manager", manager?.ToString() ?? "-", true)
            .AddField("💎 Price", CurrencyHelper.N(info.Price, Culture, currSign), true);

        var isOwnCard = targetUserId == ctx.User.Id;
        var footerParts = new List<string>
        {
            $"Fee: {info.WaifuFeePercent}%",
            $"Earned: {CurrencyHelper.N(info.TotalProduced, Culture, currSign)}"
        };

        if (isOwnCard)
        {
            var remaining = await svc.GetRemainingActionsAsync(ctx.User.Id);
            footerParts.Add($"Actions: {remaining}/{svc.MaxActions}");
        }

        eb.WithFooter(string.Join(" | ", footerParts));

        if (!string.IsNullOrEmpty(info.CustomAvatarUrl))
            eb.WithThumbnailUrl(info.CustomAvatarUrl);

        var gifts = await svc.GetGiftCountsAsync(targetUserId);
        if (gifts.Count > 0)
        {
            var giftText = string.Join("  ", gifts.Select(g => $"{g.Item.Emoji}x{g.Count}"));
            eb.AddField(GetText(strs.waifu_gifts_received), giftText, false);
        }

        var resp = Response().Embed(eb);

        if (isOwnCard)
        {
            var pending = await svc.GetPendingPayoutAsync(ctx.User.Id);
            if (pending >= 1)
                resp = resp.Interaction(CreateCollectPayoutInteraction());
        }

        await resp.SendAsync();
    }

    [Cmd]
    public async Task WaifuHelp()
    {
        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle(GetText(strs.waifu_not_participating_title))
            .WithDescription(GetText(strs.waifu_not_participating(prefix)))
            .WithFooter($"Use {prefix}cmds waifu for a list of commands");

        await Response().Embed(eb).SendAsync();
    }

    [Cmd]
    public Task Waifu([Leftover] IUser? user = null)
        => WaifuInternalAsync(user?.Id);

    [Cmd]
    public Task Waifu(ulong userId)
        => WaifuInternalAsync(userId);

    private async Task WaifuInternalAsync(ulong? targetUserId)
    {
        if (targetUserId is not null)
        {
            await ShowWaifuCardAsync(targetUserId.Value);
            return;
        }

        // No argument: check if caller is a waifu
        var isWaifu = await svc.IsWaifuAsync(ctx.User.Id);
        if (isWaifu)
        {
            await ShowWaifuCardAsync(ctx.User.Id);
            return;
        }

        // Check if caller is a fan
        var backing = await svc.GetBackingAsync(ctx.User.Id);
        if (backing.HasValue)
        {
            var backedUser = await ctx.Client.GetUserAsync(backing.Value);
            var backedName = backedUser?.ToString() ?? backing.Value.ToString();
            await Response()
                .Error(strs.waifu_fan_not_waifu(backedName))
                .Interaction(CreateOptInInteraction())
                .SendAsync();
            return;
        }

        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle(GetText(strs.waifu_not_participating_title))
            .WithDescription(GetText(strs.waifu_not_participating(prefix)));

        await Response()
            .Embed(eb)
            .Interaction(CreateOptInInteraction())
            .SendAsync();
    }

    [Cmd]
    public Task WaifuFans([Leftover] IUser? user = null)
        => WaifuFansInternalAsync(user?.Id ?? ctx.User.Id);

    [Cmd]
    public Task WaifuFans(ulong userId)
        => WaifuFansInternalAsync(userId);

    private async Task WaifuFansInternalAsync(ulong targetUserId)
    {
        var fans = await svc.GetFansAsync(targetUserId);
        if (fans.Count == 0)
        {
            await Response().Error(strs.waifu_no_fans).SendAsync();
            return;
        }

        var targetUser = await ctx.Client.GetUserAsync(targetUserId);
        var targetName = targetUser?.ToString() ?? targetUserId.ToString();

        await Response()
            .Paginated()
            .Items(fans)
            .PageSize(10)
            .Page((items, page) =>
            {
                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle(GetText(strs.waifu_fans_title(targetName)));

                var desc = string.Join("\n", items.Select((f, i) =>
                {
                    var rank = page * 10 + i + 1;
                    if (f.IsPending)
                        return $"`{rank}.` <@{f.UserId}> - {GetText(strs.waifu_fan_pending)}";
                    return $"`{rank}.` <@{f.UserId}> - {GetText(strs.waifu_fan_last_earned(f.LastCycleEarnings.ToString("N0")))}";
                }));

                return eb.WithDescription(desc);
            })
            .SendAsync();
    }

    [Cmd]
    public async Task WaifuLeaderboard()
    {
        var entries = await svc.GetLeaderboardAsync();
        if (entries.Count == 0)
        {
            await Response().Error(strs.waifu_lb_empty).SendAsync();
            return;
        }

        var currSign = cp.GetCurrencySign();

        await Response()
            .Paginated()
            .Items(entries)
            .PageSize(9)
            .Page((items, page) =>
            {
                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle(GetText(strs.waifu_lb_title));

                foreach (var (e, i) in items.Select((e, i) => (e, i)))
                {
                    var globalRank = page * 9 + i + 1;

                    int efficiency;
                    if (!e.HasManager)
                    {
                        efficiency = 0;
                    }
                    else
                    {
                        var statMultiplier = (e.Mood + e.Food) / 2000.0;
                        var capMultiplier = e.SnapshotTotalBacked > 0
                            ? Math.Min(1.0, e.ReturnsCap / (double)e.SnapshotTotalBacked)
                            : 1.0;
                        efficiency = (int)Math.Round(statMultiplier * capMultiplier * 100);
                    }

                    var value = $"{CurrencyHelper.N(e.Price, Culture, currSign)}\n"
                              + $"🆔 `{e.UserId}`\n"
                              + $"⚡ {efficiency}%\n"
                              + $"💰 {CurrencyHelper.N(e.SnapshotTotalBacked, Culture, currSign)}";

                    eb.AddField($"#{globalRank} {e.Username ?? "Unknown"}", value, true);
                }

                return eb;
            })
            .SendAsync();
    }

    [Cmd]
    public async Task WaifuOptIn()
    {
        var res = await svc.OptInAsync(ctx.User.Id);

        await res.Match(
            _ => Response().Error(strs.waifu_already_opted_in).SendAsync(),
            _ => Response().Error(strs.waifu_insufficient_funds).SendAsync(),
            _ => Response()
                .Confirm(strs.waifu_opted_in(cp.GetCurrencySign()))
                .Interaction(CreateWaifuCardInteraction())
                .SendAsync()
        );
    }

    [Cmd]
    public async Task WaifuOptOut()
    {
        var res = await svc.OptOutAsync(ctx.User.Id);

        await res.Match(
            _ => Response().Error(strs.waifu_not_opted_in).SendAsync(),
            _ => Response().Error(strs.waifu_has_fans).SendAsync(),
            _ => Response().Confirm(strs.waifu_opted_out).SendAsync()
        );
    }

    [Cmd]
    public async Task WaifuBack([Leftover] IUser? user = null)
    {
        // No args = stop backing (toggle off)
        if (user is null)
        {
            var backing = await svc.GetBackingAsync(ctx.User.Id);
            if (!backing.HasValue)
            {
                await Response().Error(strs.waifu_not_backing).SendAsync();
                return;
            }

            // Confirm stop backing
            var confirmEmbed = CreateEmbed()
                .WithPendingColor()
                .WithDescription(GetText(strs.waifu_stop_backing_confirm));

            if (!await PromptUserConfirmAsync(confirmEmbed))
                return;

            var stopRes = await svc.StopBeingFanAsync(ctx.User.Id);
            await stopRes.Match(
                _ => Response().Error(strs.waifu_not_backing).SendAsync(),
                _ => Response().Confirm(strs.waifu_stopped_backing).SendAsync()
            );
            return;
        }

        // Check if already backing someone else -> switch prompt
        var currentBacking = await svc.GetBackingAsync(ctx.User.Id);
        if (currentBacking.HasValue && currentBacking.Value != user.Id)
        {
            var currentWaifu = await ctx.Client.GetUserAsync(currentBacking.Value);
            var switchEmbed = CreateEmbed()
                .WithPendingColor()
                .WithDescription(GetText(strs.waifu_switch_confirm(
                    currentWaifu?.ToString() ?? currentBacking.Value.ToString(),
                    user.ToString())));

            if (!await PromptUserConfirmAsync(switchEmbed))
                return;
        }

        var res = await svc.BecomeFanAsync(ctx.User.Id, user.Id);

        await res.Match(
            _ => Response().Error(strs.waifu_already_backing).SendAsync(),
            _ => Response().Error(strs.waifu_not_found).SendAsync(),
            _ => Response()
                .Confirm(strs.waifu_now_backing_hint(user.ToString()))
                .Interaction(CreateBankInteraction())
                .SendAsync()
        );
    }

    [Cmd]
    public async Task WaifuBuy([Leftover] IUser user)
    {
        var res = await svc.BuyManagerAsync(ctx.User.Id, user.Id);

        await res.Match(
            _ => Response().Error(strs.waifu_outside_buy_window).SendAsync(),
            _ => Response().Error(strs.waifu_insufficient_funds).SendAsync(),
            _ => Response().Error(strs.waifu_not_found).SendAsync(),
            _ => Response().Error(strs.waifu_price_too_low).SendAsync(),
            info =>
            {
                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle(GetText(strs.waifu_bought_manager_title))
                    .WithDescription(GetText(strs.waifu_bought_manager(
                        user.ToString(),
                        info.Value.PricePaid)))
                    .AddField(GetText(strs.waifu_goes_to_waifu), $"{info.Value.WaifuPayout:N0}", true)
                    .AddField(GetText(strs.waifu_burned), $"{info.Value.Burned:N0}", true);

                if (info.Value.OldManagerId.HasValue)
                    eb.AddField(GetText(strs.waifu_old_manager_payout), $"{info.Value.OldManagerPayout:N0}", true);

                return Response().Embed(eb).SendAsync();
            }
        );
    }

    [Cmd]
    public async Task WaifuFee(int percent)
    {
        var res = await svc.SetWaifuFeeAsync(ctx.User.Id, percent);

        await res.Match(
            _ => Response().Error(strs.waifu_not_found).SendAsync(),
            _ => Response().Error(strs.waifu_not_opted_in).SendAsync(),
            _ => Response().Error(strs.waifu_invalid_fee(1, 5)).SendAsync(),
            _ => Response().Confirm(strs.waifu_fee_set(percent)).SendAsync()
        );
    }

    private async Task ImproveMoodAsync(WaifuAction action, IUser user)
    {
        var res = await svc.ImproveMoodAsync(ctx.User.Id, user.Id, action);

        await res.Match(
            _ => Response().Error(strs.waifu_self_not_allowed).SendAsync(),
            _ => Response().Error(strs.waifu_no_actions_left).SendAsync(),
            _ => Response().Error(strs.waifu_not_found).SendAsync(),
            moodIncrease =>
            {
                var successStr = action switch
                {
                    WaifuAction.Hug => strs.waifu_hugged(user.ToString(), moodIncrease.Value),
                    WaifuAction.Kiss => strs.waifu_kissed(user.ToString(), moodIncrease.Value),
                    WaifuAction.Pat => strs.waifu_patted(user.ToString(), moodIncrease.Value),
                    _ => strs.waifu_hugged(user.ToString(), moodIncrease.Value)
                };
                return Response().Confirm(successStr).SendAsync();
            }
        );
    }

    [Cmd]
    public Task Hug([Leftover] IUser user)
        => ImproveMoodAsync(WaifuAction.Hug, user);

    [Cmd]
    public Task Kiss([Leftover] IUser user)
        => ImproveMoodAsync(WaifuAction.Kiss, user);

    [Cmd]
    public Task Pat([Leftover] IUser user)
        => ImproveMoodAsync(WaifuAction.Pat, user);

    [Cmd]
    public async Task WaifuGiftShop()
    {
        var items = WaifuGiftItems.GetTodaysItems(svc.GetAllItems());
        var refreshIn = WaifuGiftItems.GetTimeUntilRefresh();
        var currSign = cp.GetCurrencySign();

        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle(GetText(strs.waifu_gift_shop_title))
            .WithFooter(GetText(strs.waifu_shop_refreshes(refreshIn.ToString(@"hh\:mm\:ss"))));

        foreach (var item in items)
        {
            var statEmoji = item.Type == GiftItemType.Food ? "🍔" : "😊";
            eb.AddField(
                $"{item.Emoji} {item.Name}",
                $"{CurrencyHelper.N(item.Price, Culture, currSign)} · +{item.Effect} {statEmoji}",
                true);
        }

        await Response().Embed(eb).SendAsync();
    }

    [Cmd]
    public async Task WaifuGift(string itemInput, [Leftover] IUser user)
    {
        var count = 1;
        var itemName = itemInput;

        var match = System.Text.RegularExpressions.Regex.Match(itemInput, @"^(\d+)[x*](.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            count = int.Parse(match.Groups[1].Value);
            itemName = match.Groups[2].Value.Trim();
        }

        var res = await svc.GiftAsync(ctx.User.Id, user.Id, itemName, count);

        await res.Match(
            _ => Response().Error(strs.waifu_self_not_allowed).SendAsync(),
            _ => Response().Error(strs.waifu_insufficient_funds).SendAsync(),
            _ => Response().Error(strs.waifu_not_found).SendAsync(),
            _ => Response().Error(strs.waifu_item_not_found(prefix)).SendAsync(),
            item => Response().Confirm(strs.waifu_gifted(
                count,
                item.Value.Emoji,
                item.Value.Name,
                user.ToString(),
                item.Value.Effect * count,
                item.Value.Type == GiftItemType.Food
                    ? GetText(strs.waifu_stat_food)
                    : GetText(strs.waifu_stat_mood)
            )).SendAsync()
        );
    }

    [Cmd]
    public async Task WaifuResign([Leftover] IUser user)
    {
        var exitInfo = await svc.GetManagerExitInfoAsync(ctx.User.Id, user.Id);
        if (exitInfo is null)
        {
            await Response().Error(strs.waifu_not_managing).SendAsync();
            return;
        }

        var embed = CreateEmbed()
            .WithPendingColor()
            .WithTitle(GetText(strs.waifu_manager_exit_title))
            .WithDescription(GetText(strs.waifu_manager_exit_warning))
            .AddField(GetText(strs.waifu_refund), $"{exitInfo.Refund:N0}", true)
            .AddField(GetText(strs.waifu_goes_to_waifu), $"{exitInfo.WaifuCut:N0}", true)
            .AddField(GetText(strs.waifu_goes_to_fans), $"{exitInfo.FanDistribution:N0}", true)
            .AddField(GetText(strs.waifu_burned), $"{exitInfo.Burned:N0}", true)
            .AddField(GetText(strs.waifu_new_price), $"{exitInfo.NewPrice:N0}", true);

        if (!await PromptUserConfirmAsync(embed))
            return;

        var res = await svc.ResignManagerAsync(ctx.User.Id, user.Id);

        await res.Match(
            _ => Response().Error(strs.waifu_not_managing).SendAsync(),
            _ => Response().Confirm(strs.waifu_resigned_manager(user.ToString())).SendAsync()
        );
    }

    [Cmd]
    public Task WaifuList([Leftover] IUser? user = null)
        => WaifuListInternalAsync(user?.Id ?? ctx.User.Id);

    [Cmd]
    public Task WaifuList(ulong userId)
        => WaifuListInternalAsync(userId);

    private async Task WaifuListInternalAsync(ulong targetUserId)
    {
        var managed = await svc.GetManagedWaifusAsync(targetUserId);
        if (managed.Count == 0)
        {
            await Response().Error(strs.waifu_managing_empty).SendAsync();
            return;
        }

        var targetUser = await ctx.Client.GetUserAsync(targetUserId);
        var targetName = targetUser?.ToString() ?? targetUserId.ToString();
        var currSign = cp.GetCurrencySign();

        await Response()
            .Paginated()
            .Items(managed)
            .PageSize(10)
            .Page((items, page) =>
            {
                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle(GetText(strs.waifu_managing_title(targetName)));

                var desc = string.Join("\n", items.Select((m, i) =>
                {
                    var name = m.Username ?? "Unknown";
                    var rank = page * 10 + i + 1;
                    return $"`{rank}.` `{m.WaifuUserId}` {name}\n　　{CurrencyHelper.N(m.Price, Culture, currSign)} | {CurrencyHelper.N(m.TotalBacked, Culture, currSign)} backed";
                }));

                return eb.WithDescription(desc);
            })
            .SendAsync();
    }

    [Cmd]
    public async Task WaifuPayout()
    {
        var pending = await svc.GetPendingPayoutAsync(ctx.User.Id);
        var currSign = cp.GetCurrencySign();

        if (pending < 1)
        {
            await Response().Error(strs.waifu_no_pending_payout).SendAsync();
            return;
        }

        await Response()
            .Confirm(strs.waifu_pending_payout(CurrencyHelper.N(pending, Culture, currSign)))
            .Interaction(CreateCollectPayoutInteraction())
            .SendAsync();
    }
}
}
