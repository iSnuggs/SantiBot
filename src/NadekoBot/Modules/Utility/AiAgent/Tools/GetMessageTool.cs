using System.Text.Json;

namespace NadekoBot.Modules.Utility.AiAgent.Tools;

/// <summary>
/// Fetches a message by ID from a specified channel.
/// Checks that the invoking user has read access to the target channel.
/// </summary>
public sealed class GetMessageTool : IAiTool, INService
{
    public string Name => "get_message";

    public string Description =>
        "Fetch the content of a Discord message by channel ID and message ID. " +
        "Returns the message text, author, and timestamp.";

    public JsonElement ParameterSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "channel_id": {
                    "type": "string",
                    "description": "The ID of the channel containing the message"
                },
                "message_id": {
                    "type": "string",
                    "description": "The ID of the message to fetch"
                }
            },
            "required": ["channel_id", "message_id"]
        }
        """).RootElement.Clone();

    public async Task<string> ExecuteAsync(AiToolContext context, JsonElement arguments)
    {
        if (!arguments.TryGetProperty("channel_id", out var channelIdEl)
            || !ulong.TryParse(channelIdEl.GetString(), out var channelId))
            return "Error: channel_id is required and must be a valid ID.";

        if (!arguments.TryGetProperty("message_id", out var messageIdEl)
            || !ulong.TryParse(messageIdEl.GetString(), out var messageId))
            return "Error: message_id is required and must be a valid ID.";

        var channel = await context.Guild.GetTextChannelAsync(channelId);
        if (channel is null)
            return "Error: Channel not found.";

        var userPerms = context.User.GetPermissions(channel);
        if (!userPerms.ViewChannel || !userPerms.ReadMessageHistory)
            return "Error: You don't have permission to read messages in that channel.";

        var message = await channel.GetMessageAsync(messageId);
        if (message is null)
            return "Error: Message not found.";

        return $"Author: {message.Author.Username}\n" +
               $"Timestamp: {message.Timestamp:yyyy-MM-dd HH:mm:ss UTC}\n" +
               $"Content:\n{message.Content}";
    }
}
