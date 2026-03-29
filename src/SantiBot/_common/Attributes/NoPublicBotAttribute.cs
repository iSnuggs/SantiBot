#nullable disable
namespace SantiBot.Common;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class NoPublicBotAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context,
        CommandInfo command,
        IServiceProvider services)
    {
#if GLOBAL_SANTI
        return Task.FromResult(PreconditionResult.FromError("Not available on the public bot. To learn how to selfhost a private bot, click [here](https://github.com/iSnuggs/SantiBot/tree/main/docs)."));
#else
        return Task.FromResult(PreconditionResult.FromSuccess());
#endif
    }
}