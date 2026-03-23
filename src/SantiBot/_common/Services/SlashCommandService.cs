#nullable disable
using Discord.Rest;
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Services;

public sealed class SlashCommandService : INService, IReadyExecutor
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commandService;
    private readonly ICommandHandler _cmdHandler;
    private readonly IServiceProvider _services;
    private readonly IBotCreds _creds;

    // Map slash command full names to text command strings
    private readonly ConcurrentDictionary<string, string> _slashToTextMap = new();

    public SlashCommandService(
        DiscordSocketClient client,
        CommandService commandService,
        ICommandHandler cmdHandler,
        IServiceProvider services,
        IBotCreds creds)
    {
        _client = client;
        _commandService = commandService;
        _cmdHandler = cmdHandler;
        _services = services;
        _creds = creds;

        _client.SlashCommandExecuted += OnSlashCommandExecuted;
    }

    public async Task OnReadyAsync()
    {
        // Wait a bit for everything to initialize
        await Task.Delay(5000);

        try
        {
            await RegisterSlashCommandsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register slash commands");
        }
    }

    private async Task RegisterSlashCommandsAsync()
    {
        var modules = _commandService.Modules.ToList();

        // Group commands by their top-level module
        var topModules = modules
            .Where(m => m.Parent is null)
            .ToList();

        var slashCommands = new List<SlashCommandBuilder>();

        foreach (var topModule in topModules)
        {
            try
            {
                var moduleName = topModule.Name?.ToLowerInvariant()?.Replace(" ", "-") ?? "general";

                // Skip owner-only modules for slash commands
                if (topModule.Preconditions.Any(p => p is OwnerOnlyAttribute))
                    continue;

                var sanitizedModuleName = SanitizeCommandName(moduleName);
                if (string.IsNullOrEmpty(sanitizedModuleName))
                    continue;

                // Collect ALL commands from this module tree (submodules + direct)
                var allSubCommands = new List<SlashCommandOptionBuilder>();
                var usedNames = new HashSet<string>();

                // Process submodules — each submodule's commands become subcommands
                foreach (var sub in topModule.Submodules)
                {
                    foreach (var cmd in sub.Commands)
                    {
                        var subCmd = BuildSubCommand(cmd, sub.Group);
                        if (subCmd is not null && usedNames.Add(subCmd.Name))
                            allSubCommands.Add(subCmd);
                    }
                }

                // Process direct commands
                foreach (var cmd in topModule.Commands)
                {
                    var subCmd = BuildSubCommand(cmd, null);
                    if (subCmd is not null && usedNames.Add(subCmd.Name))
                        allSubCommands.Add(subCmd);
                }

                if (allSubCommands.Count == 0)
                    continue;

                // Discord limit: 25 subcommands per command
                // If we have more, take the first 25
                var builder = new SlashCommandBuilder()
                    .WithName(sanitizedModuleName)
                    .WithDescription(TruncateDesc($"{topModule.Name} commands"))
                    .WithDMPermission(false);

                foreach (var subCmd in allSubCommands.Take(25))
                {
                    try
                    {
                        builder.AddOption(subCmd);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to add subcommand {Name}: {Error}", subCmd.Name, ex.Message);
                    }
                }

                if ((builder.Options?.Count ?? 0) > 0)
                    slashCommands.Add(builder);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to process module {ModuleName} for slash commands", topModule.Name);
            }
        }

        // Discord limit: 100 global commands
        slashCommands = slashCommands.Take(100).ToList();

        Log.Information("Registering {Count} slash commands...", slashCommands.Count);

        try
        {
            var props = slashCommands.Select(x => x.Build()).ToArray();
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(props);
            Log.Information("Successfully registered {Count} slash commands with {SubCount} total subcommands",
                slashCommands.Count,
                _slashToTextMap.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to bulk register slash commands");
        }
    }

    private SlashCommandBuilder BuildTopLevelCommand(CommandInfo cmd)
    {
        if (cmd.Preconditions.Any(p => p is OwnerOnlyAttribute))
            return null;

        var name = SanitizeCommandName(cmd.Aliases.FirstOrDefault() ?? cmd.Name);
        if (string.IsNullOrEmpty(name))
            return null;

        var builder = new SlashCommandBuilder()
            .WithName(name)
            .WithDescription(TruncateDesc(cmd.Summary ?? cmd.Remarks ?? $"{name} command"))
            .WithDMPermission(false);

        AddParametersToCommand(builder, cmd);

        var textCmd = cmd.Aliases.FirstOrDefault() ?? cmd.Name;
        _slashToTextMap[$"/{name}"] = textCmd;

        return builder;
    }

    private SlashCommandOptionBuilder BuildSubCommand(CommandInfo cmd, string groupPrefix)
    {
        if (cmd.Preconditions.Any(p => p is OwnerOnlyAttribute))
            return null;

        var cmdName = cmd.Aliases.FirstOrDefault() ?? cmd.Name;

        // Strip the group prefix from the command name if present
        if (!string.IsNullOrEmpty(groupPrefix) && cmdName.StartsWith(groupPrefix, StringComparison.OrdinalIgnoreCase))
            cmdName = cmdName[groupPrefix.Length..].TrimStart();

        if (string.IsNullOrEmpty(cmdName))
            cmdName = cmd.Name?.ToLowerInvariant() ?? "run";

        cmdName = SanitizeCommandName(cmdName);
        if (string.IsNullOrEmpty(cmdName))
            return null;

        var subCmd = new SlashCommandOptionBuilder()
            .WithName(cmdName)
            .WithDescription(TruncateDesc(cmd.Summary ?? cmd.Remarks ?? $"{cmdName} command"))
            .WithType(ApplicationCommandOptionType.SubCommand);

        // Add parameters
        foreach (var param in cmd.Parameters.Take(25))
        {
            if (param.Name is "args" or "_")
                continue;

            var paramName = SanitizeCommandName(param.Name);
            if (string.IsNullOrEmpty(paramName))
                continue;

            var optionType = GetOptionType(param.Type);

            try
            {
                subCmd.AddOption(
                    paramName,
                    optionType,
                    TruncateDesc(param.Summary ?? param.Name),
                    isRequired: !param.IsOptional && param.DefaultValue is null);
            }
            catch
            {
                // Skip parameters that can't be added
            }
        }

        // Build the text command mapping
        var fullTextCmd = string.IsNullOrEmpty(groupPrefix)
            ? (cmd.Aliases.FirstOrDefault() ?? cmd.Name)
            : $"{groupPrefix} {cmdName}";

        var moduleName = cmd.Module.GetTopLevelModule().Name?.ToLowerInvariant()?.Replace(" ", "-") ?? "general";
        var slashPath = string.IsNullOrEmpty(groupPrefix)
            ? $"/{SanitizeCommandName(moduleName)} {cmdName}"
            : $"/{SanitizeCommandName(moduleName)} {cmdName}";

        _slashToTextMap[slashPath] = fullTextCmd;

        return subCmd;
    }

    private void AddParametersToCommand(SlashCommandBuilder builder, CommandInfo cmd)
    {
        foreach (var param in cmd.Parameters.Take(25))
        {
            if (param.Name is "args" or "_")
                continue;

            var paramName = SanitizeCommandName(param.Name);
            if (string.IsNullOrEmpty(paramName))
                continue;

            var optionType = GetOptionType(param.Type);

            try
            {
                builder.AddOption(
                    paramName,
                    optionType,
                    TruncateDesc(param.Summary ?? param.Name),
                    isRequired: !param.IsOptional && param.DefaultValue is null);
            }
            catch
            {
                // Skip parameters that can't be added
            }
        }
    }

    private static ApplicationCommandOptionType GetOptionType(Type type)
    {
        if (type == typeof(string))
            return ApplicationCommandOptionType.String;
        if (type == typeof(int) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong))
            return ApplicationCommandOptionType.Integer;
        if (type == typeof(bool))
            return ApplicationCommandOptionType.Boolean;
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            return ApplicationCommandOptionType.Number;
        if (type == typeof(IUser) || type == typeof(IGuildUser) || type == typeof(SocketGuildUser) || type == typeof(IUser))
            return ApplicationCommandOptionType.User;
        if (type == typeof(IChannel) || type == typeof(ITextChannel) || type == typeof(SocketTextChannel))
            return ApplicationCommandOptionType.Channel;
        if (type == typeof(IRole) || type == typeof(SocketRole))
            return ApplicationCommandOptionType.Role;
        if (type == typeof(IMentionable))
            return ApplicationCommandOptionType.Mentionable;

        // Default to string for complex types (TimeSpan, enums, etc.)
        return ApplicationCommandOptionType.String;
    }

    private async Task OnSlashCommandExecuted(SocketSlashCommand cmd)
    {
        try
        {
            // Acknowledge immediately
            await cmd.DeferAsync();

            // Build the text command equivalent
            var textCommand = BuildTextCommandFromSlash(cmd);
            if (string.IsNullOrEmpty(textCommand))
            {
                await cmd.FollowupAsync("Command not recognized.", ephemeral: true);
                return;
            }

            var prefix = _cmdHandler.GetPrefix(cmd.GuildId is not null ? _client.GetGuild(cmd.GuildId.Value) : null);
            var fullCommand = prefix + textCommand;

            var channel = cmd.Channel as SocketTextChannel;
            var guild = channel?.Guild;

            // Send the command as a message for the pipeline to process
            // First, send a followup indicating the command is being processed
            var msg = await cmd.FollowupAsync($"Running `{prefix}{textCommand}`...");

            // Execute through the text command pipeline
            var context = new CommandContext(_client, new SlashCommandMessage(cmd, _client, fullCommand));
            var (success, error, info) = await (_cmdHandler as CommandHandler)!.ExecuteCommand(
                context,
                textCommand,
                _services,
                MultiMatchHandling.Best);

            if (!success && error is not null)
            {
                await cmd.FollowupAsync($"Error: {error}", ephemeral: true);
            }

            // Try to delete the "Running..." message
            try { await msg.DeleteAsync(); } catch { }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error handling slash command {CommandName}", cmd.Data.Name);
            try
            {
                await cmd.FollowupAsync("An error occurred while executing the command.", ephemeral: true);
            }
            catch { }
        }
    }

    private string BuildTextCommandFromSlash(SocketSlashCommand cmd)
    {
        var parts = new List<string>();

        // Start with the command name
        var commandName = cmd.Data.Name;
        parts.Add(commandName);

        // Walk through options to find subcommands
        var options = cmd.Data.Options?.ToList() ?? new();

        while (options.Count > 0)
        {
            var firstOpt = options[0];
            if (firstOpt.Type == ApplicationCommandOptionType.SubCommandGroup
                || firstOpt.Type == ApplicationCommandOptionType.SubCommand)
            {
                parts.Add(firstOpt.Name);
                options = firstOpt.Options?.ToList() ?? new();
            }
            else
            {
                break;
            }
        }

        // Now options contains the actual parameter values
        var paramValues = new List<string>();
        foreach (var opt in options)
        {
            var val = opt.Value?.ToString() ?? "";
            if (opt.Type == ApplicationCommandOptionType.User && opt.Value is SocketGuildUser user)
                val = user.Id.ToString();
            else if (opt.Type == ApplicationCommandOptionType.Channel && opt.Value is SocketChannel ch)
                val = ch.Id.ToString();
            else if (opt.Type == ApplicationCommandOptionType.Role && opt.Value is SocketRole role)
                val = role.Id.ToString();

            if (!string.IsNullOrEmpty(val))
                paramValues.Add(val);
        }

        // Build the full text command path
        var textPath = string.Join(" ", parts);

        // Append parameters
        if (paramValues.Count > 0)
            textPath += " " + string.Join(" ", paramValues);

        return textPath;
    }

    private static string SanitizeCommandName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // Discord requires: lowercase, 1-32 chars, only a-z, 0-9, -, _
        name = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "");

        // Remove invalid characters
        name = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

        // Must start with a letter
        while (name.Length > 0 && !char.IsLetter(name[0]))
            name = name[1..];

        // Truncate to 32 chars
        if (name.Length > 32)
            name = name[..32];

        return string.IsNullOrEmpty(name) ? null : name;
    }

    private static string TruncateDesc(string desc)
    {
        if (string.IsNullOrEmpty(desc))
            return "No description";

        return desc.Length > 100 ? desc[..97] + "..." : desc;
    }
}

/// <summary>
/// A wrapper that makes a slash command look like a user message
/// for the text command pipeline.
/// </summary>
internal sealed class SlashCommandMessage : IUserMessage
{
    private readonly SocketSlashCommand _cmd;
    private readonly DiscordSocketClient _client;
    private readonly string _content;

    public SlashCommandMessage(SocketSlashCommand cmd, DiscordSocketClient client, string content)
    {
        _cmd = cmd;
        _client = client;
        _content = content;
    }

    public string Content => _content;
    public IUser Author => _cmd.User;
    public IMessageChannel Channel => _cmd.Channel;
    public DateTimeOffset CreatedAt => _cmd.CreatedAt;
    public ulong Id => _cmd.Id;
    public MessageSource Source => MessageSource.User;
    public MessageType Type => MessageType.Default;
    public bool IsTTS => false;
    public bool IsPinned => false;
    public bool IsSuppressed => false;
    public bool MentionedEveryone => false;
    public string CleanContent => _content;
    public DateTimeOffset Timestamp => _cmd.CreatedAt;
    public DateTimeOffset? EditedTimestamp => null;
    public MessageActivity Activity => null;
    public MessageApplication Application => null;
    public MessageReference Reference => null;
    public MessageFlags? Flags => MessageFlags.None;
    public IReadOnlyCollection<IAttachment> Attachments => Array.Empty<IAttachment>();
    public IReadOnlyCollection<IEmbed> Embeds => Array.Empty<IEmbed>();
    public IReadOnlyCollection<ITag> Tags => Array.Empty<ITag>();
    public IReadOnlyCollection<ulong> MentionedChannelIds => Array.Empty<ulong>();
    public IReadOnlyCollection<ulong> MentionedRoleIds => Array.Empty<ulong>();
    public IReadOnlyCollection<ulong> MentionedUserIds => Array.Empty<ulong>();
    public IReadOnlyCollection<IMessageComponent> Components => Array.Empty<IMessageComponent>();
    public IReadOnlyCollection<IStickerItem> Stickers => Array.Empty<IStickerItem>();
    public IMessageInteractionMetadata InteractionMetadata => null;
    public MessageRoleSubscriptionData RoleSubscriptionData => null;
    public PurchaseNotification PurchaseNotification => default;
    public MessageCallData? CallData => null;
    public IMessageInteraction Interaction => null;
    public IThreadChannel Thread => null;
    public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions
        => new Dictionary<IEmote, ReactionMetadata>();

    // IUserMessage methods — most are no-ops for slash command bridge
    public Task ModifyAsync(Action<MessageProperties> func, RequestOptions options = null) => Task.CompletedTask;
    public Task PinAsync(RequestOptions options = null) => Task.CompletedTask;
    public Task UnpinAsync(RequestOptions options = null) => Task.CompletedTask;
    public Task CrosspostAsync(RequestOptions options = null) => Task.CompletedTask;
    public string Resolve(TagHandling userHandling = TagHandling.Name,
        TagHandling channelHandling = TagHandling.Name,
        TagHandling roleHandling = TagHandling.Name,
        TagHandling everyoneHandling = TagHandling.Sanitize,
        TagHandling emojiHandling = TagHandling.Name) => _content;
    public Task AddReactionAsync(IEmote emote, RequestOptions options = null) => Task.CompletedTask;
    public Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions options = null) => Task.CompletedTask;
    public Task RemoveReactionAsync(IEmote emote, ulong userId, RequestOptions options = null) => Task.CompletedTask;
    public Task RemoveAllReactionsAsync(RequestOptions options = null) => Task.CompletedTask;
    public Task RemoveAllReactionsForEmoteAsync(IEmote emote, RequestOptions options = null) => Task.CompletedTask;
    public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetReactionUsersAsync(
        IEmote emoji, int limit, RequestOptions options = null, ReactionType type = ReactionType.Normal)
        => AsyncEnumerable.Empty<IReadOnlyCollection<IUser>>();
    public Task DeleteAsync(RequestOptions options = null) => Task.CompletedTask;
    public IUserMessage ReferencedMessage => null;
    public IMessageInteraction InteractionData => null;

    public Task EndPollAsync(RequestOptions options = null) => Task.CompletedTask;
    public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetPollAnswerVotersAsync(uint answerId, int? limit = null, ulong? afterId = null, RequestOptions options = null)
        => AsyncEnumerable.Empty<IReadOnlyCollection<IUser>>();
    public MessageResolvedData ResolvedData => null;
    public Poll? Poll => null;
    public IReadOnlyCollection<MessageSnapshot> ForwardedMessages => Array.Empty<MessageSnapshot>();
}
