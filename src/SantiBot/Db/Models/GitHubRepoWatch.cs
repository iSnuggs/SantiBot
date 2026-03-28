#nullable disable
namespace SantiBot.Db.Models;

public class GitHubRepoWatch : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string RepoFullName { get; set; } = "";
    public string LastEventId { get; set; } = "";
    public bool WatchCommits { get; set; } = true;
    public bool WatchIssues { get; set; } = true;
    public bool WatchPRs { get; set; } = true;
    public bool WatchReleases { get; set; } = true;
}
