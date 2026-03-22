#nullable disable
namespace SantiBot.Modules.Music;

public interface IQueuedTrackInfo : ITrackInfo
{
    public ITrackInfo TrackInfo { get; }

    public string Queuer { get; }
}