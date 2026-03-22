#nullable disable
using SantiBot.Modules.Gambling.Common.Blackjack;

namespace SantiBot.Modules.Gambling.Services;

public class BlackJackService : INService
{
    public ConcurrentDictionary<ulong, Blackjack> Games { get; } = new();
}