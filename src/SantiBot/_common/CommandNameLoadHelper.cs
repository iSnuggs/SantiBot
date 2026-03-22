using System.Reflection;
using SantiBot.Common.Yml;
using YamlDotNet.Serialization;

namespace SantiBot.Common.Attributes;

public static class CommandNameLoadHelper
{
    private static readonly IDeserializer _deserializer = new Deserializer();

    private static readonly Lazy<Dictionary<string, string[]>> _lazyCommandAliases
        = new(() => LoadAliases());

    public static Dictionary<string, string[]> LoadAliases(string aliasesFilePath = "strings/names.yml")
    {
        var loc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var text = File.ReadAllText(Path.Combine(loc, aliasesFilePath));
        return _deserializer.Deserialize<Dictionary<string, string[]>>(text);
    }

    public static Dictionary<string, CommandStrings> LoadCommandStrings(
        string commandsFilePath = "strings/commands.yml")
    {
        var text = File.ReadAllText(commandsFilePath);

        return Yaml.Deserializer.Deserialize<Dictionary<string, CommandStrings>>(text);
    }

    public static string[] GetAliasesFor(string methodName)
        => _lazyCommandAliases.Value.TryGetValue(methodName.ToLowerInvariant(), out var aliases) && aliases.Length > 1
            ? aliases.ToArray()
            : Array.Empty<string>();

    public static string GetCommandNameFor(string methodName)
    {
        methodName = methodName.ToLowerInvariant();
        var toReturn = _lazyCommandAliases.Value.TryGetValue(methodName, out var aliases) && aliases.Length > 0
            ? aliases[0]
            : methodName;
        return toReturn;
    }
}