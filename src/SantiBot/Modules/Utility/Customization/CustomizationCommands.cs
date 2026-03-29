#nullable disable
using SantiBot.Db.Models;
using SantiBot.Modules.Utility.Customization;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Customization")]
    [Group("customize")]
    public partial class CustomizationCommands : SantiModule<CustomizationService>
    {
        // ═══════════════════════════════════════════
        //  SERVER THEMING
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Theme([Leftover] string themeName = null)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                var config = await _service.GetOrCreateAsync(ctx.Guild.Id);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"**Current Theme:** {config.ActiveTheme}\n");
                sb.AppendLine("**Available Themes:**");
                foreach (var (key, (name, emoji, color, desc)) in CustomizationService.Themes)
                {
                    var active = key == config.ActiveTheme ? " ← **ACTIVE**" : "";
                    sb.AppendLine($"{emoji} **{name}** (`{key}`) — {desc}{active}");
                }
                sb.AppendLine("\nUse `.customize theme <name>` to set!");

                var eb = CreateEmbed().WithTitle("🎨 Server Themes").WithDescription(sb.ToString()).WithOkColor();
                await Response().Embed(eb).SendAsync();
                return;
            }

            var theme = themeName.Trim().ToLower();
            if (!CustomizationService.Themes.ContainsKey(theme))
            {
                await Response().Error($"Unknown theme! Use `.customize theme` to see all options.").SendAsync();
                return;
            }

            await _service.SetThemeAsync(ctx.Guild.Id, theme);
            var t = CustomizationService.Themes[theme];
            await Response().Confirm($"{t.Emoji} Theme set to **{t.Name}**! {t.Desc}").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  CURRENCY & XP NAMING
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task CurrencyName(string name, string emoji = null)
        {
            emoji ??= "🪙";
            await _service.SetCurrencyAsync(ctx.Guild.Id, name, emoji);
            await Response().Confirm($"Currency is now called {emoji} **{name}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpName(string name, string emoji = null)
        {
            emoji ??= "⭐";
            await _service.SetXpAsync(ctx.Guild.Id, name, emoji);
            await Response().Confirm($"XP is now called {emoji} **{name}**!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  EMBED COLOR
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task EmbedColor([Leftover] string hex)
        {
            hex = hex?.Trim().TrimStart('#') ?? "00E68A";
            if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out _))
            {
                await Response().Error("Invalid hex color! Use format: `#FF5733` or `FF5733`").SendAsync();
                return;
            }
            await _service.SetEmbedColorAsync(ctx.Guild.Id, $"#{hex}");
            await Response().Confirm($"Embed color set to **#{hex}**!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  LEVEL UP MESSAGE
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task LevelUpMsg([Leftover] string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                var config = await _service.GetOrCreateAsync(ctx.Guild.Id);
                await Response().Confirm($"**Current level-up message:**\n{config.LevelUpMessage}\n\n" +
                    "**Variables:** `{user}` `{level}` `{server}`\n" +
                    "Use `.customize levelupmsg <message>` to change!").SendAsync();
                return;
            }
            await _service.SetLevelUpAsync(ctx.Guild.Id, message, 0, false);
            await Response().Confirm($"Level-up message set! Preview:\n{message.Replace("{user}", ctx.User.Mention).Replace("{level}", "10").Replace("{server}", ctx.Guild.Name)}").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  WELCOME MESSAGE
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task WelcomeMsg(string title, [Leftover] string message)
        {
            await _service.SetWelcomeAsync(ctx.Guild.Id, title, message, null);
            await Response().Confirm($"Welcome message set!\n**{title}**\n{message}").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  CUSTOM EMBEDS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task SaveEmbed(string name, string title, [Leftover] string description)
        {
            var embed = new CustomEmbed
            {
                GuildId = ctx.Guild.Id,
                Name = name,
                Title = title,
                Description = description,
                CreatedBy = ctx.User.Id,
            };
            await _service.SaveEmbedAsync(embed);
            await Response().Confirm($"Embed **{name}** saved! Use `.customize sendembed {name}` to post it.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SendEmbed([Leftover] string name)
        {
            var embed = await _service.GetEmbedAsync(ctx.Guild.Id, name);
            if (embed is null)
            {
                await Response().Error($"Embed `{name}` not found! Use `.customize embeds` to see all.").SendAsync();
                return;
            }

            var eb = CreateEmbed();
            if (!string.IsNullOrWhiteSpace(embed.Title)) eb.WithTitle(embed.Title);
            if (!string.IsNullOrWhiteSpace(embed.Description)) eb.WithDescription(embed.Description);
            if (!string.IsNullOrWhiteSpace(embed.FooterText)) eb.WithFooter(embed.FooterText);
            if (!string.IsNullOrWhiteSpace(embed.ImageUrl)) eb.WithImageUrl(embed.ImageUrl);
            if (!string.IsNullOrWhiteSpace(embed.ThumbnailUrl)) eb.WithThumbnailUrl(embed.ThumbnailUrl);
            eb.WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Embeds()
        {
            var embeds = await _service.GetEmbedsAsync(ctx.Guild.Id);
            if (embeds.Count == 0)
            {
                await Response().Confirm("No saved embeds! Admins can create one with `.customize saveembed <name> <title> <description>`").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var e in embeds)
                sb.AppendLine($"📄 **{e.Name}** — {e.Title ?? "No title"}");

            var eb = CreateEmbed()
                .WithTitle($"📋 Saved Embeds ({embeds.Count})")
                .WithDescription(sb.ToString())
                .WithFooter("Use .customize sendembed <name> to post one")
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task DeleteEmbed([Leftover] string name)
        {
            var success = await _service.DeleteEmbedAsync(ctx.Guild.Id, name);
            if (success)
                await Response().Confirm($"Embed **{name}** deleted!").SendAsync();
            else
                await Response().Error($"Embed `{name}` not found!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  CUSTOM COMMANDS
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AddCmd(string trigger, [Leftover] string response)
        {
            var cmd = new CustomCommand
            {
                GuildId = ctx.Guild.Id,
                Trigger = trigger.ToLower(),
                Response = response,
                CreatedBy = ctx.User.Id,
            };
            await _service.SaveCommandAsync(cmd);
            await Response().Confirm($"Custom command `.{trigger}` created!\nResponse: {response}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CmdList()
        {
            var commands = await _service.GetCommandsAsync(ctx.Guild.Id);
            if (commands.Count == 0)
            {
                await Response().Confirm("No custom commands! Admins can create one with `.customize addcmd <trigger> <response>`").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var c in commands.Take(25))
                sb.AppendLine($"`.{c.Trigger}` — {c.Response.Substring(0, Math.Min(50, c.Response.Length))}... (used {c.UseCount}x)");

            var eb = CreateEmbed()
                .WithTitle($"📜 Custom Commands ({commands.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task DelCmd([Leftover] string trigger)
        {
            var success = await _service.DeleteCommandAsync(ctx.Guild.Id, trigger);
            if (success)
                await Response().Confirm($"Custom command `.{trigger}` deleted!").SendAsync();
            else
                await Response().Error($"Command `.{trigger}` not found!").SendAsync();
        }

        // ═══════════════════════════════════════════
        //  OVERVIEW
        // ═══════════════════════════════════════════

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Settings()
        {
            var config = await _service.GetOrCreateAsync(ctx.Guild.Id);
            var theme = CustomizationService.Themes.TryGetValue(config.ActiveTheme, out var t) ? t : CustomizationService.Themes["default"];

            var eb = CreateEmbed()
                .WithTitle($"⚙️ {ctx.Guild.Name} — Bot Settings")
                .AddField("Theme", $"{theme.Emoji} {theme.Name}", true)
                .AddField("Currency", $"{config.CurrencyEmoji} {config.CurrencyName}", true)
                .AddField("XP", $"{config.XpEmoji} {config.XpName}", true)
                .AddField("Embed Color", config.EmbedColorHex, true)
                .AddField("Level-Up", config.LevelUpDm ? "DM" : (config.LevelUpChannelId > 0 ? $"<#{config.LevelUpChannelId}>" : "Same channel"), true)
                .AddField("Level-Up Message", config.LevelUpMessage ?? "Default", false)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }
}
