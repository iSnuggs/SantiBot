#nullable disable
using System.Diagnostics;
using System.Text;

namespace SantiBot.Modules.Utility.OpenClaw;

/// <summary>
/// Bridges SantiBot to OpenClaw's AI gateway running on the thin client.
/// Sends messages via SSH → openclaw agent CLI and returns Claude's response.
/// </summary>
public sealed class OpenClawService : INService
{
    // Connection config — thin client running OpenClaw
    private const string SSH_HOST = "100.86.110.14";
    private const string SSH_USER = "ubuntu";
    private const string SSH_PASS = "***REDACTED***";
    private const string OPENCLAW_BIN = "/home/ubuntu/.npm-global/bin/openclaw";

    // Per-user session tracking for conversation continuity
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, string> _sessions = new();

    // Rate limit — 10 second cooldown per user
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, DateTime> _cooldowns = new();
    private static readonly TimeSpan CooldownTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Send a message to Claude via OpenClaw and get a response.
    /// Each user gets their own persistent session for multi-turn conversations.
    /// </summary>
    public async Task<(bool Success, string Response)> ChatAsync(ulong userId, string message)
    {
        // Rate limit
        if (_cooldowns.TryGetValue(userId, out var last))
        {
            var wait = CooldownTime - (DateTime.UtcNow - last);
            if (wait > TimeSpan.Zero)
                return (false, $"Cooldown! Wait **{wait.Seconds}s** before asking again.");
        }
        _cooldowns[userId] = DateTime.UtcNow;

        var sessionId = _sessions.GetOrAdd(userId, _ => Guid.NewGuid().ToString());

        return await RunOpenClawAsync(message, sessionId, 120);
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
            // Escape for shell safety
            var escaped = message
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("$", "\\$")
                .Replace("`", "\\`");

            var sessionArg = sessionId != null ? $"--session-id {sessionId}" : "";

            var cmd = $"export PATH=\"$PATH:/home/ubuntu/.npm-global/bin\"; " +
                $"openclaw agent --message \"{escaped}\" {sessionArg} --timeout {timeoutSec} 2>/dev/null";

            var (success, output) = await RunSshAsync(cmd, timeoutSec + 30);

            if (!success || string.IsNullOrWhiteSpace(output))
                return (false, "Claude didn't respond. OpenClaw may be offline — check with `.oc status`");

            // Clean up the output
            var clean = output.Trim();

            // OpenClaw sometimes wraps output in ANSI codes — strip them
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\x1B\[[0-9;]*m", "");

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

    private async Task<(bool Success, string Output)> RunSshAsync(string command, int timeoutSec)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sshpass",
            Arguments = $"-p '{SSH_PASS}' ssh -o StrictHostKeyChecking=no -o ConnectTimeout=10 {SSH_USER}@{SSH_HOST} '{command}'",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

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
