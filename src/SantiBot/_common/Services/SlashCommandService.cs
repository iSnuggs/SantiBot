#nullable disable
using Discord.Rest;
using System.Globalization;
using Santi.Common.Medusa;
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Services;

public sealed class SlashCommandService : INService, IReadyExecutor
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commandService;
    private readonly ICommandHandler _cmdHandler;
    private readonly IServiceProvider _services;
    private readonly IBotCreds _creds;
    private readonly IBotStrings _strings;
    private readonly IMedusaLoaderService _medusae;

    // Map slash command full names to text command strings
    private readonly ConcurrentDictionary<string, string> _slashToTextMap = new();

    public SlashCommandService(
        DiscordSocketClient client,
        CommandService commandService,
        ICommandHandler cmdHandler,
        IServiceProvider services,
        IBotCreds creds,
        IBotStrings strings,
        IMedusaLoaderService medusae)
    {
        _client = client;
        _commandService = commandService;
        _cmdHandler = cmdHandler;
        _services = services;
        _creds = creds;
        _strings = strings;
        _medusae = medusae;

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

    // ═══════════════════════════════════════════════════════════════
    // CURATED SLASH COMMANDS — Clean, top-level commands only
    // Add new entries here to register more slash commands.
    //
    // Format: ("slash-name", "text-command", "Description", Params)
    //   Params = null → auto-detect from the text command
    //   Params = array → manually specify parameters
    // ═══════════════════════════════════════════════════════════════

    // Helper for manually specifying slash command parameters
    private sealed record SlashParam(string Name, string Description, ApplicationCommandOptionType Type, bool Required = false);

    private static readonly (string SlashName, string TextCommand, string Description, SlashParam[] Params)[] CuratedCommands =
    {
        // ── Moderation ──
        ("ban",          "ban",          "Ban a user from the server", null),
        ("unban",        "unban",        "Unban a user from the server", null),
        ("kick",         "kick",         "Kick a user from the server", null),
        ("mute",         "mute",         "Mute a user (text + voice)", null),
        ("unmute",       "unmute",       "Unmute a previously muted user", null),
        ("timeout",      "timeout",      "Timeout a user for a duration", null),
        ("warn",         "warn",         "Warn a user with optional reason", null),
        ("warnlog",      "warnlog",      "View warnings for a user", null),
        ("prune",        "prune",        "Delete messages in a channel", new SlashParam[]
        {
            new("count", "Number of messages to delete (default 100)", ApplicationCommandOptionType.Integer, Required: false),
            new("user", "Only delete messages from this user", ApplicationCommandOptionType.User, Required: false),
        }),
        ("softban",      "softban",      "Ban then unban to delete messages", null),

        // ── Music ──
        ("play",         "play",         "Play a song by name, URL, or search", new SlashParam[]
        {
            new("query", "Song name, artist, or YouTube link", ApplicationCommandOptionType.String, Required: true),
        }),
        ("queue",        "queue",        "Queue a song to play next", new SlashParam[]
        {
            new("query", "Song name, artist, or YouTube link", ApplicationCommandOptionType.String, Required: true),
        }),
        ("skip",         "next",         "Skip to the next song", null),
        ("pause",        "pause",        "Pause or resume playback", null),
        ("stop",         "stop",         "Stop music and preserve queue", null),
        ("join",         "join",         "Join your voice channel", null),
        ("volume",       "volume",       "Set music volume (0-100%)", null),
        ("nowplaying",   "nowplaying",   "Show the currently playing song", null),
        ("listqueue",    "listqueue",    "Show the current song queue", null),
        ("shuffle",      "queueshuffle", "Shuffle the song queue", null),
        ("repeat",       "queuerepeat",  "Set repeat mode (none/song/queue)", null),
        ("lyrics",       "lyrics",       "Look up lyrics for a song", null),

        // ── Economy ──
        ("cash",         "cash",         "Check your currency balance", null),
        ("give",         "give",         "Give currency to another user", null),
        ("timely",       "timely",       "Claim your periodic currency reward", null),
        ("leaderboard",  "leaderboard",  "Show the richest users", null),
        ("betroll",      "betroll",      "Bet currency on a dice roll", null),
        ("betflip",      "betflip",      "Bet on a coin flip", null),
        ("slot",         "slot",         "Play the slot machine", null),

        // ── XP & Leveling ──
        ("xp",           "experience",   "Check your XP and level", null),
        ("xplb",         "xpleaderboard","Show the server XP leaderboard", null),
        ("rank",         "experience",   "Check your rank and level", null),

        // ── Utility ──
        ("help",         "commands",     "Show bot commands and modules", null),
        ("remind",       "remind",       "Set a reminder for yourself", null),
        ("poll",         "pollcreate",   "Create a poll with button voting", null),
        ("giveaway",     "giveawaystart","Start a giveaway", null),
        ("suggest",      "suggest",      "Submit a suggestion", null),
        ("starboard",    "starboardinfo","Show starboard configuration", null),
        ("afk",          "afk",          "Set your AFK status", null),
        ("ping",         "ping",         "Check bot latency", null),
        ("userinfo",     "userinfo",     "Show info about a user", null),
        ("serverinfo",   "serverinfo",   "Show server information", null),

        // ── Games ──
        ("trivia",       "trivia",       "Start a trivia game", null),
        ("fish",         "fish",         "Go fishing!", null),
        ("hangman",      "hangman",      "Start a hangman game", null),
        ("tictactoe",    "tictactoe",    "Start a tic-tac-toe game", null),
        ("rps",          "rps",          "Play rock-paper-scissors", null),

        // ── Admin/Config ──
        ("prefix",       "prefix",       "View or change the command prefix", null),
        ("greet",        "greet",        "Toggle join announcements", null),
        ("bye",          "bye",          "Toggle leave announcements", null),
        ("setrole",      "setrole",      "Assign a role to a user", null),
        ("removerole",   "removerole",   "Remove a role from a user", null),

        // ── Searches ──
        ("anime",        "anime",        "Search for an anime on AniList", null),
        ("manga",        "manga",        "Search for a manga on AniList", null),
        ("weather",      "weather",      "Check the weather for a location", null),
        ("translate",    "translate",     "Translate text between languages", null),

        // ── Expressions ──
        ("say",          "say",          "Make the bot send a message", null),

        // ── Mod Mail ──
        ("modmail",      "modmailconfig","Show mod mail configuration", null),
        ("modmail-close","modmailclose", "Close the current mod mail thread", null),
        ("modmail-block","modmailblock", "Block a user from mod mail", null),

        // ── Dungeon RPG ──
        ("dungeon",      "dungeon enter","Start a dungeon run", new SlashParam[]
        {
            new("difficulty", "Difficulty 1-5 (default 1)", ApplicationCommandOptionType.Integer, Required: false),
        }),
        ("dungeon-profile","dungeon profile","View your RPG character profile", null),
        ("dungeon-attack","dungeon attack","Attack the current monster", null),
        ("dungeon-explore","dungeon explore","Explore the next dungeon room", null),

        // ── Raid Boss ──
        ("raidboss",     "raidboss status","Show active raid boss status", null),
        ("raidboss-attack","raidboss attack","Attack the raid boss", null),
        ("raidboss-bosses","raidboss bosses","Browse all 1000 raid bosses", null),

        // ── Pets ──
        ("pet",          "pet",          "View your active pet", null),
        ("pet-adopt",    "pet adopt",    "Adopt a new pet companion", new SlashParam[]
        {
            new("species", "Species to adopt (e.g. Dog, Cat, Dragon Hatchling)", ApplicationCommandOptionType.String, Required: true),
        }),
        ("pet-battle",   "pet battle",   "Send your pet into battle", null),

        // ── Crafting ──
        ("mine",         "craft mine",   "Mine for ores", null),
        ("chop",         "craft chop",   "Chop wood", null),
        ("cook",         "craft cook",   "Cook a recipe", new SlashParam[]
        {
            new("recipe", "Recipe name to cook", ApplicationCommandOptionType.String, Required: false),
        }),

        // ── PvP ──
        ("duel",         "pvp duel",     "Challenge someone to a 1v1 duel", new SlashParam[]
        {
            new("opponent", "User to challenge", ApplicationCommandOptionType.User, Required: true),
        }),
        ("pvp-stats",    "pvp pvpstats", "View your PvP record and Elo", null),

        // ── Fun ──
        ("8ball",        "fun eightball","Ask the magic 8-ball a question", new SlashParam[]
        {
            new("question", "Your question", ApplicationCommandOptionType.String, Required: true),
        }),
        ("joke",         "fun joke",     "Get a random joke", null),
        ("tarot",        "fun tarot",    "Draw a tarot card", null),

        // ── Social ──
        ("hug",          "rp hug",       "Hug someone", new SlashParam[]
        {
            new("user", "Who to hug", ApplicationCommandOptionType.User, Required: true),
        }),
        ("pat",          "rp pat",       "Headpat someone", new SlashParam[]
        {
            new("user", "Who to pat", ApplicationCommandOptionType.User, Required: true),
        }),

        // ── AI ──
        ("ask",          "oc ask",       "Ask Claude AI a question", new SlashParam[]
        {
            new("question", "Your question for Claude", ApplicationCommandOptionType.String, Required: true),
        }),

        // ── Achievements ──
        ("achievements", "achievements achievements","View your achievements", null),
        ("badges",       "gf badges",   "View your badges", null),
        ("battlepass",   "gf battlepass","View your battle pass progress", null),

        // ── Housing ──
        ("house",        "house myhouse","View your house", null),
        ("house-buy",    "house buy",    "Buy a house", new SlashParam[]
        {
            new("size", "House size (Cottage, Cabin, House, Manor, Mansion, Castle)", ApplicationCommandOptionType.String, Required: true),
        }),

        // ── Quests ──
        ("quests",       "quest quests", "View your active quests", null),
        ("daily-quests", "quest dailyquests","View today's daily quests", null),
    };

    private async Task RegisterSlashCommandsAsync()
    {
        var allCommands = _commandService.Commands.ToList();
        var slashCommands = new List<SlashCommandBuilder>();
        var culture = CultureInfo.GetCultureInfo("en-US");

        foreach (var (slashName, textCommand, description, manualParams) in CuratedCommands)
        {
            try
            {
                var builder = new SlashCommandBuilder()
                    .WithName(slashName)
                    .WithDescription(description)
                    .WithContextTypes(InteractionContextType.Guild);

                if (manualParams is not null)
                {
                    // Use manually specified parameters
                    foreach (var p in manualParams)
                    {
                        try
                        {
                            builder.AddOption(p.Name, p.Type, p.Description, isRequired: p.Required);
                        }
                        catch { }
                    }
                }
                else
                {
                    // Auto-detect parameters from the text command
                    var cmdInfo = allCommands
                        .Where(c => c.Aliases.Any(a => a.Equals(textCommand, StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(c => c.Parameters.Any(p => p.Type == typeof(string)) ? 1 : 0)
                        .ThenByDescending(c => c.Parameters.Count)
                        .FirstOrDefault();

                    if (cmdInfo is not null)
                    {
                        var cmdStrings = _strings.GetCommandStrings(cmdInfo.Summary, culture);
                        var yamlParams = cmdStrings?.Params;

                        foreach (var param in cmdInfo.Parameters.Take(25))
                        {
                            if (param.Name is "args" or "_")
                                continue;

                            var paramName = SanitizeCommandName(param.Name);
                            if (string.IsNullOrEmpty(paramName))
                                continue;

                            var optionType = GetOptionType(param.Type);

                            var paramDesc = param.Name;
                            if (yamlParams is not null)
                            {
                                foreach (var overload in yamlParams)
                                {
                                    if (overload.TryGetValue(param.Name, out var paramInfo) && !string.IsNullOrEmpty(paramInfo.Desc))
                                    {
                                        paramDesc = paramInfo.Desc;
                                        break;
                                    }
                                }
                            }

                            try
                            {
                                builder.AddOption(
                                    paramName,
                                    optionType,
                                    TruncateDesc(paramDesc),
                                    isRequired: !param.IsOptional && param.DefaultValue is null);
                            }
                            catch { }
                        }
                    }
                }

                slashCommands.Add(builder);
                _slashToTextMap[$"/{slashName}"] = textCommand;
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to build slash command /{SlashName}: {Error}", slashName, ex.Message);
            }
        }

        Log.Information("Registering {Count} curated slash commands...", slashCommands.Count);

        try
        {
            var props = slashCommands.Select(x => x.Build()).ToArray();
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(props);
            Log.Information("Successfully registered {Count} slash commands", slashCommands.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register slash commands");
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

    private Task OnSlashCommandExecuted(SocketSlashCommand cmd)
    {
        // Fire-and-forget on a background thread to avoid blocking the gateway
        _ = Task.Run(async () =>
        {
            try
            {
                // Build the slash path and extract parameters
                var (slashPath, paramValues) = ParseSlashCommand(cmd);

                // Look up the text command in our map
                string textCommand = null;
                if (_slashToTextMap.TryGetValue(slashPath, out var mapped))
                {
                    textCommand = mapped;
                }
                else
                {
                    // Fallback: try without the leading /
                    var pathWithoutSlash = slashPath.TrimStart('/');
                    foreach (var kvp in _slashToTextMap)
                    {
                        if (kvp.Key.TrimStart('/') == pathWithoutSlash)
                        {
                            textCommand = kvp.Value;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(textCommand))
                {
                    Log.Warning("Slash command not found in map: {SlashPath}. Available mappings: {Mappings}",
                        slashPath,
                        string.Join(", ", _slashToTextMap.Keys.Take(20)));
                    await cmd.RespondAsync("Command not recognized.", ephemeral: true);
                    return;
                }

                // Append parameter values to the text command
                if (paramValues.Count > 0)
                    textCommand += " " + string.Join(" ", paramValues);

                var prefix = _cmdHandler.GetPrefix(cmd.GuildId is not null ? _client.GetGuild(cmd.GuildId.Value) : null);
                var fullCommand = prefix + textCommand;

                Log.Information("Slash command {SlashPath} → text command: {TextCommand}", slashPath, textCommand);

                // Acknowledge the interaction with a brief message, then let the command pipeline
                // send its real response directly to the channel
                await cmd.RespondAsync($"⚡ `{prefix}{textCommand}`", ephemeral: true);

                // Execute through the text command pipeline — output goes to the channel normally
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
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error handling slash command {CommandName}", cmd.Data.Name);
                try
                {
                    if (!cmd.HasResponded)
                        await cmd.RespondAsync("An error occurred while executing the command.", ephemeral: true);
                    else
                        await cmd.FollowupAsync("An error occurred while executing the command.", ephemeral: true);
                }
                catch { }
            }
        });

        return Task.CompletedTask;
    }

    private (string slashPath, List<string> paramValues) ParseSlashCommand(SocketSlashCommand cmd)
    {
        var options = cmd.Data.Options?.ToList() ?? new();

        // Extract parameter values — put users/mentions first, then other params
        // (text commands typically expect: command @user <other args>)
        var userValues = new List<string>();
        var otherValues = new List<string>();

        foreach (var opt in options)
        {
            var val = opt.Value?.ToString() ?? "";
            if (opt.Type == ApplicationCommandOptionType.User && opt.Value is SocketGuildUser user)
            {
                val = user.Id.ToString();
                userValues.Add(val);
            }
            else if (opt.Type == ApplicationCommandOptionType.Channel && opt.Value is SocketChannel ch)
            {
                val = ch.Id.ToString();
                otherValues.Add(val);
            }
            else if (opt.Type == ApplicationCommandOptionType.Role && opt.Value is SocketRole role)
            {
                val = role.Id.ToString();
                otherValues.Add(val);
            }
            else if (!string.IsNullOrEmpty(val))
            {
                otherValues.Add(val);
            }
        }

        // Users first, then everything else
        var paramValues = new List<string>();
        paramValues.AddRange(userValues);
        paramValues.AddRange(otherValues);

        return ($"/{cmd.Data.Name}", paramValues);
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

    private string GetRealDescription(CommandInfo cmd)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            return cmd.RealSummary(_strings, _medusae, culture, "/");
        }
        catch
        {
            return null;
        }
    }

    private static string TruncateDesc(string desc)
    {
        if (string.IsNullOrEmpty(desc))
            return "No description";

        // Strip markdown formatting for cleaner slash command descriptions
        desc = desc.Replace("**", "").Replace("`", "").Replace("\n", " ").Trim();

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
