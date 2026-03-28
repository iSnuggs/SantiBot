using SantiBot.Medusa;

namespace MyMedusa;

/// <summary>
/// Your Medusa plugin for SantiBot.
///
/// Build:   dotnet publish -o bin/medusae/MyMedusa /p:DebugType=embedded
/// Install: Copy bin/medusae/MyMedusa/ to SantiBot's data/medusae/ folder
/// Load:    .meload MyMedusa
/// Unload:  .meunload MyMedusa
/// </summary>
public sealed class MyMedusaSnek : Snek
{
    public override ValueTask InitializeAsync()
    {
        Console.WriteLine("[MyMedusa] Plugin loaded!");
        return default;
    }

    public override ValueTask DisposeAsync()
    {
        Console.WriteLine("[MyMedusa] Plugin unloaded!");
        return default;
    }

    [cmd]
    public async Task Hello(AnyContext ctx)
    {
        await ctx.Channel.SendMessageAsync($"Hello from MyMedusa, {ctx.User.Mention}!");
    }

    [cmd]
    public async Task Hello(AnyContext ctx, [leftover] string name)
    {
        await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} says hello to **{name}**!");
    }
}
