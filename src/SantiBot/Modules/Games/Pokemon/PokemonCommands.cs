#nullable disable
using SantiBot.Modules.Games.Pokemon;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Pokemon")]
    [Group("pokemon")]
    public partial class PokemonCommands : SantiModule<PokemonService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Catch()
        {
            var (success, message, pokemon) = await _service.CatchAsync(ctx.User.Id);
            if (success)
            {
                var eb = CreateEmbed()
                    .WithTitle("🎯 Wild Creature Caught!")
                    .AddField("Name", pokemon.Name, true)
                    .AddField("Type", pokemon.Type, true)
                    .AddField("Level", pokemon.Level.ToString(), true)
                    .AddField("HP", pokemon.MaxHp.ToString(), true)
                    .AddField("Attack", pokemon.Attack.ToString(), true)
                    .AddField("Defense", pokemon.Defense.ToString(), true)
                    .WithOkColor();
                await Response().Embed(eb).SendAsync();
            }
            else
            {
                await Response().Error(message).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task List()
        {
            var pokemon = await _service.GetPokemonListAsync(ctx.User.Id);
            if (pokemon.Count == 0)
            {
                await Response().Error("You haven't caught any creatures! Use `.pokemon catch` to start.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"🐾 {ctx.User.Username}'s Creatures")
                .WithOkColor();

            foreach (var p in pokemon.Take(15))
                eb.AddField($"{p.Name} (Lv{p.Level})", $"{p.Type} | HP:{p.MaxHp} ATK:{p.Attack} DEF:{p.Defense}", true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Info([Leftover] string name)
        {
            var p = await _service.GetPokemonInfoAsync(ctx.User.Id, name?.Trim());
            if (p is null)
            {
                await Response().Error("Creature not found!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"📋 {p.Name}")
                .AddField("Type", p.Type, true)
                .AddField("Level", p.Level.ToString(), true)
                .AddField("HP", $"{p.Hp}/{p.MaxHp}", true)
                .AddField("Attack", p.Attack.ToString(), true)
                .AddField("Defense", p.Defense.ToString(), true)
                .AddField("XP", $"{p.Xp}/{p.XpToNext}", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Train([Leftover] string name)
        {
            var (success, message) = await _service.TrainAsync(ctx.User.Id, name?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Battle(IUser opponent)
        {
            var (success, message) = await _service.StartBattleAsync(ctx.Channel.Id, ctx.User.Id, opponent.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Attack()
        {
            var (success, message) = await _service.AttackAsync(ctx.Channel.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
