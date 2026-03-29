#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("serverbackup")]
    public partial class ServerBackupCommands : SantiModule<ServerBackupService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ServerBackup()
        {
            await Response().Confirm("Creating server backup... This may take a moment.").SendAsync();

            var backup = await _service.CreateBackupAsync(ctx.Guild, ctx.User.Id);
            await Response().Confirm($"Server backup created! ID: **#{backup.Id}**\nRestore with `.serverrestore {backup.Id}`").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ServerRestore(int backupId)
        {
            var embed = CreateEmbed()
                .WithDescription("⚠️ **DANGEROUS** - This will create missing roles, categories, and channels from the backup.\nExisting items will NOT be modified.\nType `yes` to confirm.")
                .WithErrorColor();

            await Response().Embed(embed).SendAsync();
            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            if (input?.ToUpperInvariant() != "YES")
            {
                await Response().Error("Server restore cancelled.").SendAsync();
                return;
            }

            if (await _service.RestoreBackupAsync(ctx.Guild, backupId))
                await Response().Confirm("Server restored from backup successfully!").SendAsync();
            else
                await Response().Error("Backup not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ServerBackupList()
        {
            var backups = await _service.ListBackupsAsync(ctx.Guild.Id);
            if (backups.Count == 0)
            {
                await Response().Error("No backups found.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Server Backups")
                .WithDescription(string.Join("\n", backups.Select(b => $"• **#{b.Id}** - {b.Description} ({b.DateAdded:g})")))
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }
    }
}
