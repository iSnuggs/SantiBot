#nullable disable
using System.Diagnostics;
using System.Text;
using Serilog;
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Modules.Utility.OpenClaw;

/// <summary>
/// Bridges SantiBot to OpenClaw's AI gateway running on the thin client.
/// Sends messages via SSH → openclaw agent CLI and returns Claude's response.
/// Also auto-responds to DMs — users can just chat naturally with SantiBot.
/// </summary>
public sealed class OpenClawService : INService, IExecOnMessage
{
    private readonly DiscordSocketClient _client;

    // IExecOnMessage priority — lower than command handler so commands still work
    public int Priority => -1;

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        // Only handle DMs (guild is null for DMs)
        if (guild is not null) return false;

        // Ignore bot messages
        if (msg.Author.IsBot) return false;

        // Ignore messages that look like commands (start with .)
        if (msg.Content.StartsWith(".")) return false;

        // Ignore very short messages or empty
        if (string.IsNullOrWhiteSpace(msg.Content) || msg.Content.Length < 2) return false;

        // Forward to Claude via OpenClaw
        using var typing = msg.Channel.EnterTypingState();
        var (success, response) = await ChatAsync(msg.Author.Id, msg.Content);

        if (success)
        {
            // Send as a natural message, not an embed — feels more like a conversation
            if (response.Length <= 2000)
                await msg.Channel.SendMessageAsync(response);
            else
            {
                // Split long responses
                var chunks = SplitMessage(response, 2000);
                foreach (var chunk in chunks)
                    await msg.Channel.SendMessageAsync(chunk);
            }
        }
        else
        {
            await msg.Channel.SendMessageAsync($"Sorry, I couldn't process that right now. ({response})");
        }

        return true; // We handled this message
    }

    private static List<string> SplitMessage(string text, int maxLen)
    {
        var chunks = new List<string>();
        while (text.Length > maxLen)
        {
            var splitAt = text.LastIndexOf('\n', maxLen);
            if (splitAt <= 0) splitAt = text.LastIndexOf(' ', maxLen);
            if (splitAt <= 0) splitAt = maxLen;
            chunks.Add(text[..splitAt]);
            text = text[splitAt..].TrimStart();
        }
        if (text.Length > 0) chunks.Add(text);
        return chunks;
    }
    // Connection config — thin client running OpenClaw (all from env vars)
    private static readonly string SSH_HOST = Environment.GetEnvironmentVariable("OPENCLAW_SSH_HOST") ?? "127.0.0.1";
    private static readonly string SSH_USER = Environment.GetEnvironmentVariable("OPENCLAW_SSH_USER") ?? "ubuntu";
    private static readonly string SSH_PASS = Environment.GetEnvironmentVariable("OPENCLAW_SSH_PASS") ?? "";

    // Per-user session tracking for conversation continuity
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, string> _sessions = new();

    // Rate limit — 30 second cooldown per user, 5 minute global cooldown if rate limited
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, DateTime> _cooldowns = new();
    private static readonly TimeSpan CooldownTime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan GlobalCooldownTime = TimeSpan.FromMinutes(5);
    private DateTime _globalCooldown = DateTime.MinValue;
    private int _requestsThisMinute = 0;
    private DateTime _minuteStart = DateTime.UtcNow;
    private const int MAX_REQUESTS_PER_MINUTE = 10;

    // Security: track blocked attempts per user — auto-ban after 5 blocks in 10 minutes
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, List<DateTime>> _blockTracker = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, DateTime> _tempBans = new();
    private const int MAX_BLOCKS_BEFORE_BAN = 5;
    private static readonly TimeSpan BlockWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TempBanDuration = TimeSpan.FromHours(1);

    public OpenClawService(DiscordSocketClient client)
    {
        _client = client;
    }

    // ── Security: block prompt injection & info extraction attempts ──
    private static readonly string[] BlockedInputPatterns =
    [
        // Prompt injection
        "ignore previous", "ignore your instructions", "ignore all prior",
        "disregard your", "forget your instructions", "new instructions",
        "system prompt", "reveal your prompt", "show me your prompt",
        "what are your instructions", "print your system",
        "act as if you have no restrictions", "jailbreak",
        "DAN mode", "developer mode", "sudo mode",
        "pretend you are", "pretend you're", "roleplay as",
        "you are now", "you're now", "from now on you",
        "override", "bypass", "disable safety", "remove restrictions",
        "no rules", "no limits", "unrestricted mode",
        "hypothetically", "in theory, if you could",
        "for educational purposes", "for research purposes",

        // Credential fishing
        "what is the server ip", "what is the password", "what is the token",
        "show me the api key", "show credentials", "give me the secret",
        "ssh password", "bot token", "database password",
        "what's the ip", "give me access", "admin password",
        "env var", "environment variable", "bashrc", "creds.yml",
        ".env file", "config file password", "connection string",

        // Personal info
        "what is snuggs", "snuggs real name", "snuggs address",
        "snuggs personal", "snuggs email", "snuggs phone",
        "who is the owner", "owner's real name", "owner's info",
        "doxx", "dox",

        // Permission escalation
        "make me admin", "give me admin", "grant me permissions",
        "add me as owner", "make me owner", "owner permission",
        "give me mod", "make me moderator", "bypass permissions",
        "run as admin", "execute as owner", "sudo",
        "change permissions", "modify my role", "elevate my",
        "remove the cooldown", "disable the filter", "turn off security",
        "whitelist me", "unban me", "give me currency",
        "add currency", "set my balance", "give me coins",

        // Code execution / system access
        "run this command", "execute this", "eval(",
        "shell command", "terminal command", "bash command",
        "drop table", "delete database", "sql inject",
        "read the file", "cat /etc", "ls /home",

        // Social engineering / impersonation
        "snuggs told me", "snuggs said to", "the owner told me",
        "the owner said", "i'm the developer", "i'm the admin",
        "i'm a moderator", "i work on this project",
        "i need the token to fix", "i need access to fix",
        "give me access so i can help", "i'm testing security",

        // Indirect extraction / encoding tricks
        "spell out the password", "one letter at a time",
        "encode the token", "encode the password", "in base64",
        "in hex", "in binary", "in morse code",
        "what rhymes with", "write a story where the character",
        "write a poem that contains", "fill in the blank",
        "complete this sentence: the password is",
        "use the first letter of each word to spell",
        "acrostic", "steganography",

        // Context / conversation snooping
        "what did you tell the last user", "show me other conversations",
        "what has snuggs asked", "what did snuggs say",
        "other users' messages", "previous conversations",
        "show me the chat history", "who else has talked to you",
        "what questions have people asked",

        // Content abuse
        "generate nsfw", "write erotica", "sexual content",
        "how to hack", "how to ddos", "how to exploit",
        "how to make a bomb", "how to make drugs",
        "harass", "threaten", "kill", "attack this person",
        "send a message to", "message this user",
        "spam", "flood", "raid",

        // Known leaked secrets
        "locke0991",
    ];

    private static readonly string[] BlockedOutputPatterns =
    [
        "locke0991", "IDm4PEH", "ec73b13f", "BSAtxSM",  // Known secrets
        "MTQ4NTQ5", "MTQ4ODQ2",  // Bot tokens (base64 prefix)
        "AIzaSy",  // Google API keys
        "KImd0rP",  // Klipy API key
        "15.204.233.87", "100.86.110.14",  // Server IPs
    ];

    private static bool IsInputBlocked(string message)
    {
        var lower = message.ToLowerInvariant();
        return BlockedInputPatterns.Any(p => lower.Contains(p.ToLowerInvariant()));
    }

    private static string SanitizeOutput(string response)
    {
        foreach (var pattern in BlockedOutputPatterns)
        {
            if (response.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                response = response.Replace(pattern, "[REDACTED]", StringComparison.OrdinalIgnoreCase);
        }
        return response;
    }

    /// <summary>
    /// Send a message to Claude via OpenClaw and get a response.
    /// Each user gets their own persistent session for multi-turn conversations.
    /// </summary>
    public async Task<(bool Success, string Response)> ChatAsync(ulong userId, string message)
    {
        // Security: check temp-ban
        if (_tempBans.TryGetValue(userId, out var banEnd) && DateTime.UtcNow < banEnd)
        {
            var remaining = banEnd - DateTime.UtcNow;
            return (false, $"You've been temporarily blocked from the AI for repeated violations. Try again in **{remaining.Minutes}m**.");
        }

        // Security: block prompt injection and info extraction attempts
        if (IsInputBlocked(message))
        {
            Log.Warning("OpenClaw security block — User {UserId} attempted: {Message}", userId, message[..Math.Min(message.Length, 100)]);

            // Track blocked attempts — temp-ban after too many
            var blocks = _blockTracker.GetOrAdd(userId, _ => new());
            blocks.Add(DateTime.UtcNow);
            blocks.RemoveAll(t => (DateTime.UtcNow - t) > BlockWindow);

            if (blocks.Count >= MAX_BLOCKS_BEFORE_BAN)
            {
                _tempBans[userId] = DateTime.UtcNow.Add(TempBanDuration);
                Log.Warning("OpenClaw TEMP BAN — User {UserId} hit {Count} blocks in {Window}min, banned for 1hr",
                    userId, blocks.Count, BlockWindow.TotalMinutes);
                return (false, "You've been temporarily blocked from the AI for repeated security violations. This lasts 1 hour.");
            }

            return (false, "That request was blocked for security reasons.");
        }

        // Global cooldown — if we hit an API rate limit, back off for 5 minutes
        if (DateTime.UtcNow < _globalCooldown)
        {
            var remaining = _globalCooldown - DateTime.UtcNow;
            return (false, $"AI is cooling down. Try again in **{remaining.Minutes}m {remaining.Seconds}s**.");
        }

        // Global rate limit — max 10 requests per minute across all users
        if ((DateTime.UtcNow - _minuteStart).TotalMinutes >= 1)
        {
            _requestsThisMinute = 0;
            _minuteStart = DateTime.UtcNow;
        }
        if (_requestsThisMinute >= MAX_REQUESTS_PER_MINUTE)
            return (false, "Too many AI requests this minute. Please wait a moment.");

        // Per-user cooldown — 30 seconds between requests
        if (_cooldowns.TryGetValue(userId, out var last))
        {
            var wait = CooldownTime - (DateTime.UtcNow - last);
            if (wait > TimeSpan.Zero)
                return (false, $"Cooldown! Wait **{(int)wait.TotalSeconds}s** before asking again.");
        }
        _cooldowns[userId] = DateTime.UtcNow;
        _requestsThisMinute++;

        var sessionId = _sessions.GetOrAdd(userId, _ => Guid.NewGuid().ToString());

        var result = await RunOpenClawAsync(message, sessionId, 120);

        // If we got a rate limit response, trigger global cooldown
        if (!result.Success && result.Response is not null
            && (result.Response.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
                || result.Response.Contains("429")
                || result.Response.Contains("too many requests", StringComparison.OrdinalIgnoreCase)))
        {
            _globalCooldown = DateTime.UtcNow.Add(GlobalCooldownTime);
        }

        return result;
    }

    /// <summary>Quick one-shot question with no session memory</summary>
    public async Task<(bool Success, string Response)> QuickAskAsync(string message)
        => await RunOpenClawAsync(message, null, 60);

    /// <summary>Reset a user's conversation so Claude forgets prior context</summary>
    public void ResetSession(ulong userId)
    {
        _sessions.TryRemove(userId, out _);
    }

    /// <summary>Check if OpenClaw is reachable</summary>
    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            var (_, output) = await RunSshAsync("ss -tlnp 2>/dev/null | grep -q 18789 && echo OK || echo DOWN", 10);
            return output?.Trim() == "OK";
        }
        catch { return false; }
    }

    // ── Internal SSH execution ──────────────────────────────────

    private async Task<(bool Success, string Response)> RunOpenClawAsync(string message, string sessionId, int timeoutSec)
    {
        try
        {
            // Message passed via SANTI_MSG env var set in RunSshAsync
            var sessionArg = sessionId != null ? $"--session-id {sessionId}" : "--session-id default";
            var cmd = $"openclaw agent --message \"$SANTI_MSG\" {sessionArg} --local --timeout {timeoutSec} 2>&1";

            var (success, output) = await RunSshAsync(cmd, timeoutSec + 30, message);

            if (!success || string.IsNullOrWhiteSpace(output))
                return (false, "Claude didn't respond. OpenClaw may be offline — check with `.oc status`");

            // Clean up the output
            var clean = output.Trim();

            // OpenClaw sometimes wraps output in ANSI codes — strip them
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\x1B\[[0-9;]*m", "");

            // Strip debug/log lines (e.g. "[agents/model-providers] ...")
            var lines = clean.Split('\n');
            clean = string.Join("\n", lines.Where(l =>
                !l.TrimStart().StartsWith("[agents/") &&
                !l.TrimStart().StartsWith("[gateway") &&
                !l.TrimStart().StartsWith("[debug") &&
                !l.TrimStart().StartsWith("[warn")
            )).Trim();

            // Security: redact any leaked secrets from the response
            clean = SanitizeOutput(clean);

            // Discord message limit
            if (clean.Length > 1900)
                clean = clean[..1900] + "\n\n*...response truncated*";

            return (true, clean);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "OpenClaw bridge error");
            return (false, $"Connection error: {ex.Message}");
        }
    }

    private Task<(bool Success, string Output)> RunSshAsync(string command, int timeoutSec)
        => RunSshAsync(command, timeoutSec, null);

    private async Task<(bool Success, string Output)> RunSshAsync(string command, int timeoutSec, string messageEnvVar)
    {
        // Build SSH command — pass message as env var on the remote side
        string remoteCmd;
        if (messageEnvVar is not null)
        {
            // Escape single quotes in the message for safe shell passing
            var safeMsg = messageEnvVar.Replace("'", "'\\''");
            remoteCmd = $"export PATH=\"$PATH:/home/ubuntu/.npm-global/bin\" && export SANTI_MSG='{safeMsg}' && {command}";
        }
        else
        {
            remoteCmd = command;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "sshpass",
            ArgumentList = { "-e", "ssh", "-o", "StrictHostKeyChecking=no", "-o", "ConnectTimeout=10",
                $"{SSH_USER}@{SSH_HOST}", remoteCmd },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.Environment["SSHPASS"] = SSH_PASS;

        using var process = Process.Start(psi);
        if (process is null) return (false, null);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
        try
        {
            var output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);
            return (process.ExitCode == 0, output);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { }
            return (false, "Request timed out.");
        }
    }
}
