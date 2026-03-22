#nullable disable
using SantiBot.Modules.Gambling.Common.AnimalRacing;

namespace SantiBot.Modules.Gambling.Services;

public class AnimalRaceService : INService
{
    public ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new();
}