#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Db;

public static class SantiExpressionExtensions
{
    public static int ClearFromGuild(this DbSet<SantiExpression> exprs, ulong guildId)
        => exprs.Delete(x => x.GuildId == guildId);

    public static IEnumerable<SantiExpression> ForId(this DbSet<SantiExpression> exprs, ulong id)
        => exprs.AsNoTracking().AsQueryable().Where(x => x.GuildId == id).ToList();
}