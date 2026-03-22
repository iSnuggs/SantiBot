namespace SantiBot.Db.Models;

#nullable disable
public sealed class GiveawayModel
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong MessageId { get; set; }
    public ulong ChannelId { get; set; }
    public string Message { get; set; }

    public IList<GiveawayUser> Participants { get; set; } = new List<GiveawayUser>();
    public DateTime EndsAt { get; set; }

    // SantiBot additions
    public int WinnerCount { get; set; } = 1;
    public ulong? RequiredRoleId { get; set; }
}