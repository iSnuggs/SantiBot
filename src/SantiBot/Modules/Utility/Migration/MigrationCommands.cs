#nullable disable
using System.Text;
using SantiBot.Modules.Utility.Migration;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Migration")]
    [Group("migrate")]
    public partial class MigrationCommands : SantiModule<MigrationService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ImportMee6()
        {
            var data = await ReadAttachment();
            if (data is null) return;

            var msg = await Response().Confirm("Importing MEE6 levels... this may take a moment.").SendAsync();

            var (imported, skipped, errors) = await _service.ImportMee6Levels(ctx.Guild.Id, data);

            await msg.DeleteAsync();
            await Response()
                .Confirm($"MEE6 import complete!\n"
                    + $"**Imported:** {imported} users\n"
                    + $"**Skipped:** {skipped}\n"
                    + $"**Errors:** {errors}")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ImportDyno()
        {
            var data = await ReadAttachment();
            if (data is null) return;

            var msg = await Response().Confirm("Importing Dyno levels... this may take a moment.").SendAsync();

            var (imported, skipped, errors) = await _service.ImportDynoLevels(ctx.Guild.Id, data);

            await msg.DeleteAsync();
            await Response()
                .Confirm($"Dyno import complete!\n"
                    + $"**Imported:** {imported} users\n"
                    + $"**Skipped:** {skipped}\n"
                    + $"**Errors:** {errors}")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ImportCarl()
        {
            var data = await ReadAttachment();
            if (data is null) return;

            var msg = await Response().Confirm("Importing Carl-bot levels... this may take a moment.").SendAsync();

            var (imported, skipped, errors) = await _service.ImportCarlLevels(ctx.Guild.Id, data);

            await msg.DeleteAsync();
            await Response()
                .Confirm($"Carl-bot import complete!\n"
                    + $"**Imported:** {imported} users\n"
                    + $"**Skipped:** {skipped}\n"
                    + $"**Errors:** {errors}")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ImportCsv(string userIdCol = "user_id", string xpCol = "xp", string levelCol = "level")
        {
            var data = await ReadAttachment();
            if (data is null) return;

            var msg = await Response().Confirm("Importing CSV levels... this may take a moment.").SendAsync();

            var (imported, skipped, errors) = await _service.ImportGenericCsv(
                ctx.Guild.Id, data, userIdCol, xpCol, levelCol);

            await msg.DeleteAsync();
            await Response()
                .Confirm($"CSV import complete!\n"
                    + $"**Imported:** {imported} users\n"
                    + $"**Skipped:** {skipped}\n"
                    + $"**Errors:** {errors}")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ExportLevels()
        {
            var json = await _service.ExportLevels(ctx.Guild.Id);

            if (string.IsNullOrEmpty(json) || json == "{\"players\":[]}")
            {
                await Response().Error("No level data found for this server.").SendAsync();
                return;
            }

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await ctx.Channel.SendFileAsync(
                stream,
                $"santibot_levels_{ctx.Guild.Id}.json",
                "Here's your level data export!");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Preview(string format = "mee6")
        {
            var data = await ReadAttachment();
            if (data is null) return;

            var result = _service.MigrationPreview(ctx.Guild.Id, data, format);

            if (result.ParseError)
            {
                await Response().Error("Could not parse the file. Make sure the format is correct.").SendAsync();
                return;
            }

            if (result.UserCount == 0)
            {
                await Response().Error("No valid entries found in the file.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("Migration Preview")
                .WithOkColor()
                .AddField("Format", format.ToUpperInvariant(), true)
                .AddField("Users Found", result.UserCount.ToString(), true)
                .AddField("Total XP", result.TotalXp.ToString("N0"), true)
                .AddField("Highest Level", result.HighestLevel.ToString(), true)
                .AddField("Lowest Level", result.LowestLevel.ToString(), true)
                .AddField("Average Level", result.AverageLevel.ToString(), true)
                .WithFooter("Run the import command to apply these changes.");

            await Response().Embed(eb).SendAsync();
        }

        // ── Helper ───────────────────────────────────────────────

        private async Task<string> ReadAttachment()
        {
            var attachment = ctx.Message.Attachments.FirstOrDefault();
            if (attachment is null)
            {
                await Response().Error("Attach a file to import!").SendAsync();
                return null;
            }

            // Limit to 10 MB to prevent abuse
            if (attachment.Size > 10 * 1024 * 1024)
            {
                await Response().Error("File is too large! Maximum size is 10 MB.").SendAsync();
                return null;
            }

            try
            {
                using var http = new HttpClient();
                return await http.GetStringAsync(attachment.Url);
            }
            catch
            {
                await Response().Error("Could not download the attachment.").SendAsync();
                return null;
            }
        }
    }
}
