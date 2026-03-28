namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("webhook")]
    [Name("WebhookRelay")]
    public partial class WebhookRelayCommands : SantiModule<WebhookRelayService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WebhookCreate(ITextChannel channel)
        {
            var config = await _service.CreateEndpointAsync(ctx.Guild.Id, channel.Id);
            var url = $"https://your-bot-domain/api/webhook/{config.EndpointId}";
            await Response()
                .Confirm(strs.webhook_created(config.EndpointId, channel.Mention, url))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WebhookList()
        {
            var endpoints = await _service.ListEndpointsAsync(ctx.Guild.Id);
            if (endpoints.Count == 0)
            {
                await Response().Error(strs.webhook_none).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Webhook Relay Endpoints");

            foreach (var ep in endpoints)
            {
                eb.AddField(ep.EndpointId,
                    $"Channel: <#{ep.TargetChannelId}> | Enabled: {(ep.Enabled ? "Yes" : "No")} | Secret: {(string.IsNullOrEmpty(ep.SecretKey) ? "None" : "Set")}",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WebhookRemove(string endpointId)
        {
            var removed = await _service.RemoveEndpointAsync(ctx.Guild.Id, endpointId);
            if (removed)
                await Response().Confirm(strs.webhook_removed(endpointId)).SendAsync();
            else
                await Response().Error(strs.webhook_not_found(endpointId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WebhookSecret(string endpointId, [Leftover] string secret)
        {
            var updated = await _service.SetSecretAsync(ctx.Guild.Id, endpointId, secret);
            if (updated)
                await Response().Confirm(strs.webhook_secret_set(endpointId)).SendAsync();
            else
                await Response().Error(strs.webhook_not_found(endpointId)).SendAsync();
        }
    }
}
