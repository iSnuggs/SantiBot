namespace SantiBot;

public interface ISantiInteractionService
{
    public SantiInteractionBase Create(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true,
        bool clearAfter = true);

    public SantiInteractionBase Create<T>(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, T, Task> onTrigger,
        in T state,
        bool singleUse = true,
        bool clearAfter = true);

    SantiInteractionBase Create(
        ulong userId,
        SelectMenuBuilder menu,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true);
    
    SantiInteractionBase Create(
        ulong userId, 
        ButtonBuilder button,
        ModalBuilder modal,
        Func<SocketModal, Task> onTrigger,
        bool singleUse = true);
    
}