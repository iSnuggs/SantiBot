using System.ComponentModel.DataAnnotations;

namespace SantiBot.Db.Models;

public class HoneypotChannel
{
    [Key]
    public ulong GuildId { get; set; }
    
    public ulong ChannelId { get; set; }
}