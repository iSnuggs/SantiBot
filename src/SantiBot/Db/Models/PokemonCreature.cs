#nullable disable
namespace SantiBot.Db.Models;

public class UserPokemon : DbEntity
{
    public ulong UserId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // Fire, Water, Grass, Electric
    public int Level { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Xp { get; set; }
    public int XpToNext { get; set; }
}
