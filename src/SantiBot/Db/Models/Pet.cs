#nullable disable
namespace SantiBot.Db.Models;

public class Pet : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string Name { get; set; } = "";
    public string Species { get; set; } = "Dog";
    public string Emoji { get; set; } = "\ud83d\udc36";
    public int Level { get; set; } = 1;
    public long Xp { get; set; }
    public int Happiness { get; set; } = 50;
    public int Hunger { get; set; } = 50;
    public int Energy { get; set; } = 100;
    public bool IsShiny { get; set; }
    public int Strength { get; set; }
    public int Agility { get; set; }
    public int Intelligence { get; set; }
    public int AdventureCount { get; set; }
    public int BattlesWon { get; set; }
    public DateTime LastFedAt { get; set; }
    public DateTime LastPlayedAt { get; set; }
    public int EvolutionStage { get; set; } = 1;
}

public enum PetRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

public record PetSpeciesData(
    string Name,
    string Emoji,
    int BaseStrength,
    int BaseAgility,
    int BaseIntelligence,
    string Evo2Name,
    string Evo2Emoji,
    string Evo3Name,
    string Evo3Emoji,
    PetRarity Rarity);
