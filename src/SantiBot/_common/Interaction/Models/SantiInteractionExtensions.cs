namespace SantiBot;

public static class SantiInteractionExtensions
{
    public static MessageComponent CreateComponent(
        this SantiInteractionBase interactionBase
    )
    {
        var cb = new ComponentBuilder();

        interactionBase.AddTo(cb);

        return cb.Build();
    }
}