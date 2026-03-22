using SantiBot.Db.Models;

namespace SantiBot.Modules.Searches.Common;

public static class Extensions
{
    public static StreamDataKey CreateKey(this FollowedStream fs)
        => new(fs.Type, fs.Username);
}