#nullable disable
using System.Text;
using SantiBot.Modules.Gambling.Loans;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Loan")]
    [Group("loan")]
    public partial class LoanCommands2 : SantiModule<LoanService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Take(long amount)
        {
            var (success, message) = await _service.TakeLoanAsync(ctx.Guild.Id, ctx.User.Id, amount);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Repay(long? amount = null)
        {
            var (success, message) = await _service.RepayLoanAsync(ctx.Guild.Id, ctx.User.Id, amount);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Status()
        {
            var loan = await _service.GetLoanStatusAsync(ctx.Guild.Id, ctx.User.Id);
            if (loan is null)
            {
                await Response().Confirm("You have no active loans.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("🏦 Loan Status")
                .AddField("Principal", $"{loan.Principal} 🥠", true)
                .AddField("Amount Owed", $"{loan.AmountOwed} 🥠", true)
                .AddField("Credit Score", loan.CreditScore.ToString(), true)
                .AddField("Taken At", loan.TakenAt.ToString("yyyy-MM-dd HH:mm"), true)
                .AddField("Interest", "10% per day", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task History()
        {
            var history = await _service.GetHistoryAsync(ctx.Guild.Id, ctx.User.Id);
            if (history.Count == 0)
            {
                await Response().Error("No loan history.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var h in history)
                sb.AppendLine($"{h.Amount} 🥠 — {(h.RepaidOnTime ? "✅ On Time" : "❌ Late")} — {h.DateAdded:yyyy-MM-dd}");

            await Response().Confirm($"📜 Loan History\n```\n{sb}\n```").SendAsync();
        }
    }
}
