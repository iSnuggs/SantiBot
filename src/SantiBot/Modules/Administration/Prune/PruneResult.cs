#nullable disable
namespace SantiBot.Modules.Administration.Services;

public enum PruneResult
{
    Success,
    AlreadyRunning,
    FeatureLimit,
}