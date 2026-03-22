using SantiBot.Services.Currency;

namespace SantiBot.Services;

public interface ITxTracker
{
    Task TrackAdd(ulong userId, long amount, TxData? txData);
    Task TrackRemove(ulong userId, long amount, TxData? txData);
}