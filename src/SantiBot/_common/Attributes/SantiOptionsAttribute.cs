namespace SantiBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SantiOptionsAttribute<TOption> : Attribute
    where TOption: ISantiCommandOptions
{
}