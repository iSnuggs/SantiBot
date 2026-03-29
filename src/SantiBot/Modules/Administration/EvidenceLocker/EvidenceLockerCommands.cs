#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("evidence")]
    public partial class EvidenceLockerCommands : SantiModule<EvidenceLockerService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task EvidenceAdd(int caseId, string url, [Leftover] string note = null)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                await Response().Error("Invalid URL.").SendAsync();
                return;
            }

            var evidence = await _service.AddEvidenceAsync(ctx.Guild.Id, caseId, url, note, ctx.User.Id);
            await Response().Confirm($"Evidence **#{evidence.Id}** added to case **#{caseId}**.\nURL: {url}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task EvidenceList(int caseId)
        {
            var items = await _service.ListEvidenceAsync(ctx.Guild.Id, caseId);
            if (items.Count == 0)
            {
                await Response().Error($"No evidence found for case **#{caseId}**.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle($"Evidence for Case #{caseId}")
                .WithOkColor();

            foreach (var e in items)
            {
                embed.AddField($"#{e.Id} - {e.DateAdded:g} by <@{e.AddedByUserId}>",
                    $"[Link]({e.Url}){(string.IsNullOrEmpty(e.Note) ? "" : $"\n{e.Note}")}");
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task EvidenceDel(int id)
        {
            if (await _service.RemoveEvidenceAsync(ctx.Guild.Id, id))
                await Response().Confirm($"Evidence **#{id}** deleted.").SendAsync();
            else
                await Response().Error("Evidence not found.").SendAsync();
        }
    }
}
