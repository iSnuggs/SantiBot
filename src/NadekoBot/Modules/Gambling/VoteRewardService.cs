using System.Globalization;
using Grpc.Core;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.GrpcVotesApi;

namespace NadekoBot.Modules.Gambling.Services;

public class VoteRewardService(
    ShardData shardData,
    GamblingConfigService gcs,
    GamblingService gs,
    CurrencyService cs,
    IBotCache cache,
    DiscordSocketClient client,
    IMessageSenderService sender,
    IBotCreds creds
) : INService, IReadyExecutor
{
    private TypedKey<DateTime> VoteKey(ulong userId)
        => new($"vote:{userId}");

    private Server? _app;
    private IMessageChannel? _voteFeedChannel;

    public async Task OnReadyAsync()
    {
        if (shardData.ShardId != 0)
            return;

        if (creds.Votes is null || creds.Votes.Host is null || creds.Votes.Port == 0)
            return;

        var serverCreds = ServerCredentials.Insecure;
        var ssd = VoteService.BindService(new VotesGrpcService(this));

        _app = new()
        {
            Ports =
            {
                new(creds.Votes.Host, creds.Votes.Port, serverCreds),
            }
        };

        _app.Services.Add(ssd);
        _app.Start();

        if (gcs.Data.VoteFeedChannelId is ulong cid)
        {
            _voteFeedChannel = await client.GetChannelAsync(cid) as IMessageChannel;
        }
    }

    public void SetVoiceChannel(IMessageChannel? channel)
    {
        gcs.ModifyConfig(c => { c.VoteFeedChannelId = channel?.Id; });
        _voteFeedChannel = channel;
    }

    public async Task UserVotedAsync(ulong userId, VoteType requestType)
    {
        var gcsData = gcs.Data;
        var reward = gcsData.VoteReward;
        if (reward <= 0)
            return;

        var key = VoteKey(userId);
        if (!await cache.AddAsync(key, DateTime.UtcNow, expiry: TimeSpan.FromHours(6)))
        {
            Log.Information("User {UserId} has already voted in the last 6 hours", userId);
            return;
        }

        (reward, var msg) = await gs.GetAmountAndMessage(userId, reward);
        await cs.AddAsync(userId, reward, new("vote", requestType.ToString()));

        _ = Task.Run(async () =>
        {
            try
            {
                var user = await client.GetUserAsync(userId);

                await sender.Response(user)
                    .Confirm(strs.vote_reward(N(reward)) + "\n\n" + msg)
                    .SendAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to send vote confirmation message to user {UserId}", userId);
            }
        });

        _ = Task.Run(async () =>
        {
            if (_voteFeedChannel is not null)
            {
                try
                {
                    var user = await client.GetUserAsync(userId);
                    await _voteFeedChannel.SendMessageAsync(
                        $"{user} just received {strs.vote_reward(N(reward))} for voting!",
                        allowedMentions: AllowedMentions.None);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Unable to send vote reward message to user {UserId}", userId);
                }
            }
        });
    }

    public async Task<TimeSpan?> LastVoted(ulong userId)
    {
        var key = VoteKey(userId);
        var last = await cache.GetAsync(key);
        return last.Match(
            static x => DateTime.UtcNow.Subtract(x),
            static _ => default(TimeSpan?));
    }

    private string N(long amount)
        => CurrencyHelper.N(amount, CultureInfo.InvariantCulture, gcs.Data.Currency.Sign);
}

public sealed class VotesGrpcService(VoteRewardService vrs)
    : VoteService.VoteServiceBase, INService
{
    public override async Task<GrpcVoteResult> VoteReceived(GrpcVoteData request, ServerCallContext context)
    {
        await vrs.UserVotedAsync(ulong.Parse(request.UserId), request.Type);

        return new();
    }
}