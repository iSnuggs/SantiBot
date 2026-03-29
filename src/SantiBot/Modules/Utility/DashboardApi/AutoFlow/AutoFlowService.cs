#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using System.Text.Json;
using AutoFlowModel = SantiBot.Db.Models.AutoFlow;

namespace SantiBot.Modules.Utility.DashboardApi.AutoFlow;

/// <summary>
/// Bot response builder -- visual flow builder backend.
/// Stores trigger->response chains as JSON.
/// API: GET/POST/DELETE /api/guild/{guildId}/autoflows
/// </summary>
public sealed class AutoFlowService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly ShardData _shardData;
    private NonBlocking.ConcurrentDictionary<ulong, List<AutoFlowModel>> _flows = new();

    public AutoFlowService(DbService db, DiscordSocketClient client,
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
        var flows = await uow.GetTable<AutoFlowModel>()
            .Where(x => x.IsEnabled &&
                        Queries.GuildOnShard(x.GuildId, _shardData.TotalShards, _shardData.ShardId))
            .ToListAsyncLinqToDB();

        _flows = flows
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
                if (!_flows.TryGetValue(textChannel.Guild.Id, out var flows))
                    return;

                foreach (var flow in flows)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(flow.FlowJson);
                        var root = doc.RootElement;

                        if (!root.TryGetProperty("trigger", out var trigger))
                            continue;

                        var triggerType = trigger.GetProperty("type").GetString();
                        var triggerValue = trigger.GetProperty("value").GetString();

                        var matches = triggerType switch
                        {
                            "contains" => msg.Content.Contains(triggerValue, StringComparison.OrdinalIgnoreCase),
                            "startswith" => msg.Content.StartsWith(triggerValue, StringComparison.OrdinalIgnoreCase),
                            "exact" => msg.Content.Equals(triggerValue, StringComparison.OrdinalIgnoreCase),
                            "regex" => System.Text.RegularExpressions.Regex.IsMatch(msg.Content, triggerValue),
                            _ => false
                        };

                        if (!matches) continue;

                        if (!root.TryGetProperty("response", out var response))
                            continue;

                        var responseType = response.GetProperty("type").GetString();
                        var responseValue = response.GetProperty("value").GetString();

                        // Replace placeholders
                        responseValue = responseValue
                            .Replace("{user}", msg.Author.Mention)
                            .Replace("{channel}", textChannel.Mention)
                            .Replace("{server}", textChannel.Guild.Name);

                        if (responseType == "embed")
                        {
                            var embed = _sender.CreateEmbed()
                                .WithDescription(responseValue)
                                .WithOkColor();
                            await _sender.Response(textChannel).Embed(embed).SendAsync();
                        }
                        else
                        {
                            await textChannel.SendMessageAsync(responseValue);
                        }
                    }
                    catch { /* skip broken flows */ }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error in AutoFlow handler");
            }
        });

        return Task.CompletedTask;
    }

    public async Task<int> CreateFlowAsync(ulong guildId, string name, string flowJson = null)
    {
        flowJson ??= JsonSerializer.Serialize(new
        {
            trigger = new { type = "contains", value = "" },
            response = new { type = "text", value = "" }
        });

        await using var uow = _db.GetDbContext();
        var flow = await uow.GetTable<AutoFlowModel>()
            .InsertWithOutputAsync(() => new AutoFlowModel
            {
                GuildId = guildId,
                Name = name,
                FlowJson = flowJson,
                IsEnabled = true
            });

        _flows.AddOrUpdate(guildId, [flow], (_, list) =>
        {
            list.Add(flow);
            return list;
        });

        return flow.Id;
    }

    public async Task<List<AutoFlowModel>> ListFlowsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<AutoFlowModel>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> DeleteFlowAsync(ulong guildId, string name)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<AutoFlowModel>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .DeleteAsync();

        if (count > 0 && _flows.TryGetValue(guildId, out var flows))
            flows.RemoveAll(f => f.Name == name);

        return count > 0;
    }
}
