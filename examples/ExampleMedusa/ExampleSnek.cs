using SantiBot.Medusa;

namespace ExampleMedusa;

/// <summary>
/// Example Medusa plugin for SantiBot.
///
/// This is a minimal reference showing how to build a Medusa plugin.
/// A "Snek" is the main class that SantiBot loads — it holds your commands
/// and lifecycle hooks.
///
/// To use:
///   1. Build: dotnet publish -o bin/medusae/ExampleMedusa /p:DebugType=embedded
///   2. Copy bin/medusae/ExampleMedusa/ to your SantiBot's data/medusae/ folder
///   3. Load: .meload ExampleMedusa
///   4. Try: .greet or .greet @someone
///   5. Unload: .meunload ExampleMedusa
/// </summary>
public sealed class ExampleSnek : Snek
{
    // Called once when the plugin is loaded
    public override ValueTask InitializeAsync()
    {
        Console.WriteLine("[ExampleMedusa] Loaded successfully!");
        return default;
    }

    // Called when the plugin is unloaded
    public override ValueTask DisposeAsync()
    {
        Console.WriteLine("[ExampleMedusa] Unloaded!");
        return default;
    }

    // A simple command that greets the user
    [cmd]
    public async Task Greet(AnyContext ctx)
    {
        await ctx.Channel.SendMessageAsync($"Hey there, {ctx.User.Mention}! I'm an example Medusa plugin.");
    }

    // A command that greets a mentioned user
    [cmd]
    public async Task Greet(AnyContext ctx, [leftover] string name)
    {
        await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} says hello to **{name}**!");
    }

    // A command that shows the current server time
    [cmd]
    public async Task ServerTime(AnyContext ctx)
    {
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        await ctx.Channel.SendMessageAsync($"Current server time: **{now}**");
    }
}
