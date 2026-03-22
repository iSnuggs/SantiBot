using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SixLabors.ImageSharp;

namespace SantiBot.Modules.Xp;

public partial class Xp
{
    [Group("rankcard")]
    [Name("RankCard")]
    public partial class RankCardCommands : SantiModule
    {
        private readonly DbService _db;

        public RankCardCommands(DbService db)
        {
            _db = db;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task RankCardBg([Leftover] string? url = null)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                // Reset background
                await using var uow = _db.GetDbContext();
                await uow.GetTable<UserXpCard>()
                    .Where(x => x.UserId == ctx.User.Id && x.GuildId == ctx.Guild.Id)
                    .Set(x => x.BackgroundUrl, (string?)null)
                    .UpdateAsync();

                await Response().Confirm(strs.rankcard_bg_reset).SendAsync();
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                await Response().Error(strs.rankcard_invalid_url).SendAsync();
                return;
            }

            await using (var uow = _db.GetDbContext())
            {
                var existing = await uow.GetTable<UserXpCard>()
                    .FirstOrDefaultAsyncLinqToDB(x => x.UserId == ctx.User.Id && x.GuildId == ctx.Guild.Id);

                if (existing is null)
                {
                    await uow.GetTable<UserXpCard>()
                        .InsertAsync(() => new UserXpCard
                        {
                            UserId = ctx.User.Id,
                            GuildId = ctx.Guild.Id,
                            BackgroundUrl = url,
                        });
                }
                else
                {
                    await uow.GetTable<UserXpCard>()
                        .Where(x => x.Id == existing.Id)
                        .Set(x => x.BackgroundUrl, url)
                        .UpdateAsync();
                }
            }

            await Response().Confirm(strs.rankcard_bg_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task RankCardColor([Leftover] string? hexColor = null)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
            {
                await using var uow = _db.GetDbContext();
                await uow.GetTable<UserXpCard>()
                    .Where(x => x.UserId == ctx.User.Id && x.GuildId == ctx.Guild.Id)
                    .Set(x => x.AccentColor, (string?)null)
                    .UpdateAsync();

                await Response().Confirm(strs.rankcard_color_reset).SendAsync();
                return;
            }

            hexColor = hexColor.TrimStart('#');
            if (hexColor.Length != 6 || !int.TryParse(hexColor, System.Globalization.NumberStyles.HexNumber, null, out _))
            {
                await Response().Error(strs.rankcard_invalid_color).SendAsync();
                return;
            }

            await using (var uow = _db.GetDbContext())
            {
                var existing = await uow.GetTable<UserXpCard>()
                    .FirstOrDefaultAsyncLinqToDB(x => x.UserId == ctx.User.Id && x.GuildId == ctx.Guild.Id);

                if (existing is null)
                {
                    await uow.GetTable<UserXpCard>()
                        .InsertAsync(() => new UserXpCard
                        {
                            UserId = ctx.User.Id,
                            GuildId = ctx.Guild.Id,
                            AccentColor = "#" + hexColor,
                        });
                }
                else
                {
                    await uow.GetTable<UserXpCard>()
                        .Where(x => x.Id == existing.Id)
                        .Set(x => x.AccentColor, "#" + hexColor)
                        .UpdateAsync();
                }
            }

            await Response().Confirm(strs.rankcard_color_set("#" + hexColor)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task RankCardReset()
        {
            await using var uow = _db.GetDbContext();
            await uow.GetTable<UserXpCard>()
                .Where(x => x.UserId == ctx.User.Id && x.GuildId == ctx.Guild.Id)
                .DeleteAsync();

            await Response().Confirm(strs.rankcard_reset).SendAsync();
        }
    }
}
