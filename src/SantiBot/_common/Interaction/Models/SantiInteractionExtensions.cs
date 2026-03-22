namespace SantiBot;

public static class SantiInteractionExtensions
{
    public static MessageComponent CreateComponent(
        this SantiInteractionBase nadekoInteractionBase
    )
    {
        var cb = new ComponentBuilder();

        nadekoInteractionBase.AddTo(cb);

        return cb.Build();
    }
}