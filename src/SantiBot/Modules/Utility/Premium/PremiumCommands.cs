#nullable disable
using SantiBot.Modules.Utility.Premium;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Premium")]
    [Group("premium")]
    public partial class PremiumCommands : SantiModule<PremiumService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PremiumInfo()
        {
            var pg = await _service.GetOrCreateAsync(ctx.Guild.Id);
            var isPremium = await _service.IsPremiumAsync(ctx.Guild.Id);

            var tierEmoji = PremiumTiers.GetTierEmoji(pg.Tier);
            var (cmds, embeds, feeds) = PremiumTiers.GetLimits(pg.Tier);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{tierEmoji} Premium Status — {ctx.Guild.Name}")
                .AddField("Current Tier", $"**{pg.Tier}**", true)
                .AddField("Status", isPremium ? "Active" : "Free Plan", true);

            if (pg.ExpiresAt.HasValue)
                eb.AddField("Expires", $"<t:{((DateTimeOffset)pg.ExpiresAt.Value).ToUnixTimeSeconds()}:R>", true);

            if (pg.PremiumSince.HasValue)
                eb.AddField("Premium Since", $"<t:{((DateTimeOffset)pg.PremiumSince.Value).ToUnixTimeSeconds()}:D>", true);

            var limitsDisplay = pg.Tier == PremiumTiers.Enterprise
                ? "Custom Commands: **Unlimited**\nSaved Embeds: **Unlimited**\nRSS Feeds: **Unlimited**"
                : $"Custom Commands: **{cmds}**\nSaved Embeds: **{embeds}**\nRSS Feeds: **{feeds}**";

            eb.AddField("Limits", limitsDisplay, false);

            // Active features
            var enabledFeatures = pg.Features?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
            if (enabledFeatures.Length > 0)
            {
                var featureNames = enabledFeatures
                    .Select(f => Premium.PremiumFeatures.All.FirstOrDefault(a => a.feature == f))
                    .Where(f => f.feature is not null)
                    .Select(f => $"\u2705 {f.displayName}");

                eb.AddField("Active Features", string.Join('\n', featureNames), false);
            }
            else
            {
                eb.AddField("Active Features", "None — upgrade to unlock features!", false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PremiumFeatures()
        {
            var currentTier = await _service.GetTierAsync(ctx.Guild.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Premium Features")
                .WithDescription(
                    $"Your current tier: **{currentTier}**\n\n" +
                    PremiumService.FormatFeatureList())
                .WithFooter("Features with your tier or lower are available to you.");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PremiumCompare()
        {
            var currentTier = await _service.GetTierAsync(ctx.Guild.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Premium Tier Comparison")
                .WithDescription(
                    $"Your current tier: **{currentTier}**\n\n" +
                    PremiumService.FormatTierComparison());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task PremiumSet(string tier, int days = 30)
        {
            if (!PremiumTiers.AllTiers.Contains(tier))
            {
                await Response()
                    .Error($"Invalid tier. Valid tiers: {string.Join(", ", PremiumTiers.AllTiers)}")
                    .SendAsync();
                return;
            }

            if (days < 1 || days > 3650)
            {
                await Response().Error("Duration must be between 1 and 3650 days.").SendAsync();
                return;
            }

            var expiresAt = tier == PremiumTiers.Free
                ? (DateTime?)null
                : DateTime.UtcNow.AddDays(days);

            await _service.SetTierAsync(ctx.Guild.Id, tier, expiresAt);

            var tierEmoji = PremiumTiers.GetTierEmoji(tier);
            if (tier == PremiumTiers.Free)
            {
                await Response().Confirm($"{tierEmoji} This server has been set to the **Free** plan.").SendAsync();
            }
            else
            {
                await Response()
                    .Confirm($"{tierEmoji} This server has been upgraded to **{tier}** for **{days}** days!")
                    .SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PremiumCheck(string feature)
        {
            var hasFeature = await _service.HasFeatureAsync(ctx.Guild.Id, feature);
            var featureDef = Premium.PremiumFeatures.All.FirstOrDefault(f => f.feature == feature);

            if (featureDef.feature is null)
            {
                await Response()
                    .Error($"Unknown feature `{feature}`.\nValid features: {string.Join(", ", Premium.PremiumFeatures.All.Select(f => $"`{f.feature}`"))}")
                    .SendAsync();
                return;
            }

            if (hasFeature)
            {
                await Response()
                    .Confirm($"\u2705 **{featureDef.displayName}** is available on this server!")
                    .SendAsync();
            }
            else
            {
                await Response()
                    .Error($"\u274c **{featureDef.displayName}** requires **{featureDef.minTier}** tier or higher.")
                    .SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PremiumLimits()
        {
            var pg = await _service.GetOrCreateAsync(ctx.Guild.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Premium Limits — {pg.Tier} Tier");

            if (pg.Tier == PremiumTiers.Enterprise)
            {
                eb.WithDescription("You are on the **Enterprise** plan. All limits are **unlimited**!");
            }
            else
            {
                eb.AddField("Custom Commands", $"{pg.MaxCustomCommands}", true)
                    .AddField("Saved Embeds", $"{pg.MaxSavedEmbeds}", true)
                    .AddField("RSS Feeds", $"{pg.MaxFeeds}", true);

                if (pg.Tier != PremiumTiers.Pro)
                {
                    var nextTier = pg.Tier == PremiumTiers.Free ? PremiumTiers.Basic : PremiumTiers.Pro;
                    var (nextCmds, nextEmbeds, nextFeeds) = PremiumTiers.GetLimits(nextTier);
                    eb.AddField($"Upgrade to {nextTier}",
                        $"Custom Commands: {nextCmds}\nSaved Embeds: {nextEmbeds}\nRSS Feeds: {nextFeeds}",
                        false);
                }
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
