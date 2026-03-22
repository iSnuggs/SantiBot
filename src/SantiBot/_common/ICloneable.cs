#nullable disable
namespace SantiBot.Common;

public interface ICloneable<T>
    where T : new()
{
    public T Clone();
}