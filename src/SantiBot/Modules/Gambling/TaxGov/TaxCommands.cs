#nullable disable
using System.Text;
using SantiBot.Modules.Gambling.TaxGov;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Tax")]
    [Group("tax")]
    public partial class TaxCommands : SantiModule<TaxService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Rate()
        {
            var rate = await _service.GetTaxRateAsync(ctx.Guild.Id);
            await Response().Confirm($"Current tax rate: **{rate}%**").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Set(int percentage)
        {
            var (success, message) = await _service.SetTaxRateAsync(ctx.Guild.Id, ctx.User.Id, percentage);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }

    [Name("Election")]
    [Group("election")]
    public partial class ElectionCommands : SantiModule<TaxService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Start()
        {
            var (success, message) = await _service.StartElectionAsync(ctx.Guild.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Vote(IUser user)
        {
            var (success, message) = await _service.VoteAsync(ctx.Guild.Id, ctx.User.Id, user.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Results()
        {
            var results = await _service.GetElectionResultsAsync(ctx.Guild.Id);
            if (results.Count == 0)
            {
                await Response().Error("No votes yet!").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var (candidateId, votes) in results.OrderByDescending(x => x.Value))
                sb.AppendLine($"<@{candidateId}> — {votes} vote(s)");

            var eb = CreateEmbed()
                .WithTitle("📊 Election Results")
                .WithDescription(sb.ToString())
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }

    [Name("Treasury")]
    [Group("treasury")]
    public partial class TreasuryCommands : SantiModule<TaxService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Treasury()
        {
            var gov = await _service.GetGovInfoAsync(ctx.Guild.Id);
            var eb = CreateEmbed()
                .WithTitle("🏛️ Government Info")
                .AddField("Tax Rate", $"{gov.TaxRate}%", true)
                .AddField("Treasury", $"{gov.Treasury} 🥠", true)
                .AddField("Elected Official", gov.ElectedOfficialId == 0 ? "None" : $"<@{gov.ElectedOfficialId}>", true)
                .AddField("Election Active", gov.ElectionActive ? "Yes" : "No", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }
}
