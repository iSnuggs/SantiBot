namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("PluginMarket")]
    [Group("pluginmarket")]
    public partial class PluginMarketCommands : SantiModule<PluginMarket.PluginMarketService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PluginMarketSearch([Leftover] string query = "")
        {
            var plugins = _service.SearchPlugins(query);
            if (plugins.Count == 0)
            {
                await Response().Error("No plugins found.").SendAsync();
                return;
            }

            var desc = string.Join("\n\n", plugins.Take(10).Select(p =>
                $"**{p.Name}** v{p.Version} by *{p.Author}*\n{p.Description}\nTags: {string.Join(", ", p.Tags.Select(t => $"`{t}`"))}"));

            await Response()
                .Embed(CreateEmbed().WithOkColor()
                    .WithTitle("Plugin Marketplace")
                    .WithDescription(desc)
                    .WithFooter($"{plugins.Count} plugins found"))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PluginMarketInfo(string name)
        {
            var plugin = _service.GetPluginInfo(name);
            if (plugin is null)
            {
                await Response().Error("Plugin not found.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Plugin: {plugin.Name}")
                .AddField("Version", plugin.Version, true)
                .AddField("Author", plugin.Author, true)
                .AddField("Description", plugin.Description, false)
                .AddField("Tags", string.Join(", ", plugin.Tags.Select(t => $"`{t}`")), false)
                .WithFooter("Use .pluginmarket install <name> to install");

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task PluginMarketInstall(string name)
        {
            var success = await _service.InstallPluginAsync(ctx.Guild.Id, name);
            if (success)
                await Response().Confirm($"Plugin **{name}** installed successfully.").SendAsync();
            else
                await Response().Error("Plugin not found or already installed.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task PluginMarketUninstall(string name)
        {
            var success = await _service.UninstallPluginAsync(ctx.Guild.Id, name);
            if (success)
                await Response().Confirm($"Plugin **{name}** uninstalled.").SendAsync();
            else
                await Response().Error("Plugin not installed.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PluginMarketInstalled()
        {
            var installed = await _service.ListInstalledAsync(ctx.Guild.Id);
            if (installed.Count == 0)
            {
                await Response().Confirm("No plugins installed.").SendAsync();
                return;
            }

            var desc = string.Join("\n", installed.Select(p =>
                $"**{p.PluginName}** v{p.Version} [{(p.IsEnabled ? "ON" : "OFF")}]"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Installed Plugins").WithDescription(desc))
                .SendAsync();
        }
    }
}
