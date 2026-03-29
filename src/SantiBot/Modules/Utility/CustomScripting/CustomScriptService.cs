#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.RegularExpressions;

namespace SantiBot.Modules.Utility.CustomScripting;

/// <summary>
/// Custom command scripting with simple template language.
/// Syntax: {user}, {channel}, {server}, {random:1-100}, {pick:a|b|c}, {if:condition:then:else}
/// </summary>
public sealed class CustomScriptService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly ShardData _shardData;
    private NonBlocking.ConcurrentDictionary<ulong, List<CustomScript>> _scripts = new();
    private static readonly Random _rng = new();

    public CustomScriptService(DbService db, DiscordSocketClient client,
        IMessageSenderService sender, ShardData shardData)
    {
        _db = db;
        _client = client;
        _sender = sender;
        _shardData = shardData;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var scripts = await uow.GetTable<CustomScript>()
            .Where(x => x.IsEnabled &&
                        Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
            .ToListAsyncLinqToDB();

        _scripts = scripts
            .GroupBy(x => x.GuildId)
            .ToDictionary(x => x.Key, x => x.ToList())
            .ToConcurrent();

        _client.MessageReceived += HandleMessage;
    }

    private Task HandleMessage(SocketMessage msg)
    {
        if (msg is not SocketUserMessage userMsg || msg.Author.IsBot)
            return Task.CompletedTask;
        if (msg.Channel is not SocketTextChannel textChannel)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                if (!_scripts.TryGetValue(textChannel.Guild.Id, out var scripts))
                    return;

                foreach (var script in scripts)
                {
                    if (!msg.Content.Equals(script.Trigger, StringComparison.OrdinalIgnoreCase) &&
                        !msg.Content.StartsWith(script.Trigger + " ", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var args = msg.Content.Length > script.Trigger.Length
                        ? msg.Content[(script.Trigger.Length + 1)..]
                        : "";

                    var result = ExecuteScript(script.Script, msg.Author, textChannel, args);
                    if (!string.IsNullOrEmpty(result))
                        await textChannel.SendMessageAsync(result);
                    break;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error in custom script handler");
            }
        });

        return Task.CompletedTask;
    }

    public static string ExecuteScript(string script, IUser user, ITextChannel channel, string args)
    {
        var result = script;

        // {user} - mention
        result = result.Replace("{user}", user.Mention);
        result = result.Replace("{user.name}", user.Username);
        result = result.Replace("{user.id}", user.Id.ToString());

        // {channel}
        result = result.Replace("{channel}", channel.Mention);
        result = result.Replace("{channel.name}", channel.Name);

        // {server}
        result = result.Replace("{server}", channel.Guild.Name);

        // {args}
        result = result.Replace("{args}", args);

        // {random:min-max}
        result = Regex.Replace(result, @"\{random:(\d+)-(\d+)\}", m =>
        {
            var min = int.Parse(m.Groups[1].Value);
            var max = int.Parse(m.Groups[2].Value);
            return _rng.Next(min, max + 1).ToString();
        });

        // {pick:a|b|c}
        result = Regex.Replace(result, @"\{pick:([^}]+)\}", m =>
        {
            var options = m.Groups[1].Value.Split('|');
            return options[_rng.Next(options.Length)];
        });

        // {if:condition:then:else} - simple equality check
        result = Regex.Replace(result, @"\{if:([^:]*):([^:]*):([^}]*)\}", m =>
        {
            var condition = m.Groups[1].Value.Trim();
            var thenVal = m.Groups[2].Value;
            var elseVal = m.Groups[3].Value;

            // Check if args match the condition
            if (string.IsNullOrEmpty(condition))
                return string.IsNullOrEmpty(args) ? thenVal : elseVal;

            return args.Contains(condition, StringComparison.OrdinalIgnoreCase) ? thenVal : elseVal;
        });

        // {time} - current UTC time
        result = result.Replace("{time}", DateTime.UtcNow.ToString("HH:mm UTC"));
        result = result.Replace("{date}", DateTime.UtcNow.ToString("yyyy-MM-dd"));

        return result;
    }

    public async Task<bool> AddScriptAsync(ulong guildId, string trigger, string script)
    {
        trigger = trigger.ToLower().Trim();
        await using var uow = _db.GetDbContext();
        var exists = await uow.GetTable<CustomScript>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.Trigger == trigger);
        if (exists) return false;

        var cs = await uow.GetTable<CustomScript>()
            .InsertWithOutputAsync(() => new CustomScript
            {
                GuildId = guildId,
                Trigger = trigger,
                Script = script,
                IsEnabled = true
            });

        _scripts.AddOrUpdate(guildId, [cs], (_, list) =>
        {
            list.Add(cs);
            return list;
        });

        return true;
    }

    public async Task<bool> RemoveScriptAsync(ulong guildId, string trigger)
    {
        trigger = trigger.ToLower().Trim();
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<CustomScript>()
            .Where(x => x.GuildId == guildId && x.Trigger == trigger)
            .DeleteAsync();

        if (count > 0 && _scripts.TryGetValue(guildId, out var scripts))
            scripts.RemoveAll(s => s.Trigger == trigger);

        return count > 0;
    }

    public async Task<List<CustomScript>> ListScriptsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<CustomScript>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
