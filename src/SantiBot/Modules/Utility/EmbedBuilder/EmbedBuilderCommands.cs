#nullable disable
using SantiBot.Common;
using System.Text.Json;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class EmbedBuilderCommands : SantiModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task Embed(ITextChannel channel, [Leftover] string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                await Response().Error(strs.embed_no_json).SendAsync();
                return;
            }

            try
            {
                var embed = BuildEmbedFromJson(json);
                await channel.SendMessageAsync(embed: embed.Build());
                await Response().Confirm(strs.embed_sent(channel.Mention)).SendAsync();
            }
            catch (Exception ex)
            {
                await Response().Error(strs.embed_invalid_json(ex.Message)).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task EmbedSimple(ITextChannel channel, string title, [Leftover] string description)
        {
            var embed = CreateEmbed()
                .WithTitle(title)
                .WithDescription(description)
                .WithOkColor();

            await channel.SendMessageAsync(embed: embed.Build());
            await Response().Confirm(strs.embed_sent(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task EmbedColor(ITextChannel channel, string hexColor, string title, [Leftover] string description)
        {
            if (!TryParseColor(hexColor, out var color))
            {
                await Response().Error(strs.embed_invalid_color).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color);

            await channel.SendMessageAsync(embed: embed.Build());
            await Response().Confirm(strs.embed_sent(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task EmbedEdit(ITextChannel channel, ulong messageId, [Leftover] string json)
        {
            try
            {
                var msg = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (msg is null || msg.Author.Id != ctx.Client.CurrentUser.Id)
                {
                    await Response().Error(strs.embed_cant_edit).SendAsync();
                    return;
                }

                var embed = BuildEmbedFromJson(json);
                await msg.ModifyAsync(m => m.Embed = embed.Build());
                await Response().Confirm(strs.embed_edited).SendAsync();
            }
            catch (Exception ex)
            {
                await Response().Error(strs.embed_invalid_json(ex.Message)).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task EmbedTemplate()
        {
            var template = """
                {
                  "title": "Your Title Here",
                  "description": "Your description here",
                  "color": "#0C95E9",
                  "fields": [
                    { "name": "Field 1", "value": "Value 1", "inline": true },
                    { "name": "Field 2", "value": "Value 2", "inline": true }
                  ],
                  "footer": "Footer text",
                  "thumbnail": "https://example.com/image.png",
                  "image": "https://example.com/large-image.png"
                }
                """;

            await Response().Confirm(strs.embed_template(Format.Code(template, "json"))).SendAsync();
        }

        private static EmbedBuilder BuildEmbedFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var embed = new EmbedBuilder();

            if (root.TryGetProperty("title", out var title))
                embed.WithTitle(title.GetString());

            if (root.TryGetProperty("description", out var desc))
                embed.WithDescription(desc.GetString());

            if (root.TryGetProperty("color", out var color))
            {
                var colorStr = color.GetString();
                if (TryParseColor(colorStr, out var parsedColor))
                    embed.WithColor(parsedColor);
            }

            if (root.TryGetProperty("url", out var url))
                embed.WithUrl(url.GetString());

            if (root.TryGetProperty("footer", out var footer))
                embed.WithFooter(footer.GetString());

            if (root.TryGetProperty("thumbnail", out var thumb))
                embed.WithThumbnailUrl(thumb.GetString());

            if (root.TryGetProperty("image", out var image))
                embed.WithImageUrl(image.GetString());

            if (root.TryGetProperty("author", out var author))
                embed.WithAuthor(author.GetString());

            if (root.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Array)
            {
                foreach (var field in fields.EnumerateArray())
                {
                    var name = field.TryGetProperty("name", out var n) ? n.GetString() : "—";
                    var value = field.TryGetProperty("value", out var v) ? v.GetString() : "—";
                    var inline = field.TryGetProperty("inline", out var i) && i.GetBoolean();
                    embed.AddField(name, value, inline);
                }
            }

            return embed;
        }

        private static bool TryParseColor(string input, out Color color)
        {
            color = default;
            if (string.IsNullOrEmpty(input))
                return false;

            input = input.TrimStart('#');
            if (uint.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out var hex))
            {
                color = new Color(hex);
                return true;
            }

            return false;
        }
    }
}
