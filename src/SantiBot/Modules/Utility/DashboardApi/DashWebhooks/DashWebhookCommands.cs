namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("DashWebhooks")]
    [Group("dashwebhook")]
    public partial class DashWebhookCommands : SantiModule<DashboardApi.DashWebhooks.DashWebhookService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task DashWebhookAdd(string url, [Leftover] string eventName)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                await Response().Error("Invalid webhook URL.").SendAsync();
                return;
            }

            var validEvents = new[] { "config_changed", "member_warned", "member_banned", "role_changed", "command_used" };
            if (!validEvents.Contains(eventName.ToLower()))
            {
                await Response().Error($"Invalid event. Valid events: {string.Join(", ", validEvents.Select(e => $"`{e}`"))}").SendAsync();
                return;
            }

            var id = await _service.AddWebhookAsync(ctx.Guild.Id, url, eventName);
            await Response()
                .Confirm($"Dashboard webhook #{id} added for event `{eventName}`")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task DashWebhookRemove(int id)
        {
            var success = await _service.RemoveWebhookAsync(ctx.Guild.Id, id);
            if (success)
                await Response().Confirm($"Webhook #{id} removed.").SendAsync();
            else
                await Response().Error("Webhook not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task DashWebhookList()
        {
            var hooks = await _service.ListWebhooksAsync(ctx.Guild.Id);
            if (hooks.Count == 0)
            {
                await Response().Error("No dashboard webhooks configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", hooks.Select(h =>
                $"`#{h.Id}` `{h.Event}` -> {h.Url.TrimTo(40)} [{(h.IsEnabled ? "ON" : "OFF")}]"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Dashboard Webhooks").WithDescription(desc))
                .SendAsync();
        }
    }
}
