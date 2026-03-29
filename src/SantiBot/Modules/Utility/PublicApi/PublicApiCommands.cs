namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("PublicApi")]
    [Group("apikey")]
    public partial class PublicApiCommands : SantiModule<PublicApi.PublicApiService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ApiKeyGenerate()
        {
            var key = await _service.GenerateApiKeyAsync(ctx.Guild.Id, ctx.User.Id);

            // DM the key to the user for security
            try
            {
                var dm = await ctx.User.CreateDMChannelAsync();
                await dm.SendMessageAsync($"Your SantiBot API key for **{ctx.Guild.Name}**:\n```\n{key}\n```\n" +
                    "Keep this secret! Use it in the `X-Api-Key` header for API requests.\n" +
                    "Endpoints:\n" +
                    $"- `GET /api/public/v1/guild/{ctx.Guild.Id}/leaderboard`\n" +
                    $"- `GET /api/public/v1/guild/{ctx.Guild.Id}/stats`\n" +
                    $"- `GET /api/public/v1/guild/{ctx.Guild.Id}/user/{{userId}}`");

                await Response().Confirm("API key generated and sent to your DMs.").SendAsync();
            }
            catch
            {
                await Response().Error("Could not DM you. Please enable DMs and try again.").SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ApiKeyRevoke()
        {
            var success = await _service.RevokeApiKeyAsync(ctx.Guild.Id, ctx.User.Id);
            if (success)
                await Response().Confirm("Your API key has been revoked.").SendAsync();
            else
                await Response().Error("No active API key found.").SendAsync();
        }
    }
}
