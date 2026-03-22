using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Services;

public interface ICustomBehavior
    : IExecOnMessage,
        IInputTransformer,
        IExecPreCommand,
        IExecNoCommand,
        IExecPostCommand
{

}