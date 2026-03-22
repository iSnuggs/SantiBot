#nullable disable
using System.ComponentModel.DataAnnotations;

namespace SantiBot.Db.Models;

public class Quote : DbEntity
{
    public ulong GuildId { get; set; }

    [Required]
    public string Keyword { get; set; }

    [Required]
    public string AuthorName { get; set; }

    public ulong AuthorId { get; set; }

    [Required]
    public string Text { get; set; }

    // SantiBot additions
    public int UseCount { get; set; }
}

public enum OrderType
{
    Id = -1,
    Keyword = -2
}