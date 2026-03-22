#nullable enable
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

// using YamlDotNet.Core;
// using YamlDotNet.Serialization;

namespace SantiBot.Generators
{
    internal readonly struct TranslationPair
    {
        public string Name { get; }
        public string Value { get; }

        public TranslationPair(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    [Generator]
    public class LocalizedStringsGenerator : ISourceGenerator
    {
//         private const string LOC_STR_SOURCE = @"namespace SantiBot
// {
//     public readonly struct LocStr
//     {
//         public readonly string Key;
//         public readonly object[] Params;
//         
//         public LocStr(string key, params object[] data)
//         {
//             Key = key;
//             Params = data;
//         }
//     }
// }";

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var mergedDict = new Dictionary<string, string>();
            
            foreach (var additionalFile in context.AdditionalFiles)
            {
                if (Path.GetFileName(additionalFile.Path) != "res.yml")
                    continue;
                
                var fields = GetFields(additionalFile.GetText()?.ToString());
                foreach (var field in fields)
                {
                    mergedDict[field.Name] = field.Value;
                }
            }

            using (var stringWriter = new StringWriter())
            using (var sw = new IndentedTextWriter(stringWriter))
            {
                sw.WriteLine("#pragma warning disable CS8981");
                sw.WriteLine("namespace SantiBot;");
                sw.WriteLine();

                sw.WriteLine("public static class strs");
                sw.WriteLine("{");
                sw.Indent++;

                var typedParamStrings = new List<string>(10);
                foreach (var field in mergedDict)
                {
                    var matches = Regex.Matches(field.Value, @"{(?<num>\d)[}:]");
                    var max = 0;
                    foreach (Match match in matches)
                    {
                        max = Math.Max(max, int.Parse(match.Groups["num"].Value) + 1);
                    }

                    typedParamStrings.Clear();
                    var typeParams = new string[max];
                    var passedParamString = string.Empty;
                    for (var i = 0; i < max; i++)
                    {
                        typedParamStrings.Add($"in T{i} p{i}");
                        passedParamString += $", p{i}";
                        typeParams[i] = $"T{i}";
                    }

                    var sig = string.Empty;
                    var typeParamStr = string.Empty;
                    if (max > 0)
                    {
                        sig = $"({string.Join(", ", typedParamStrings)})";
                        typeParamStr = $"<{string.Join(", ", typeParams)}>";
                    }

                    sw.WriteLine("public static LocStr {0}{1}{2} => new LocStr(\"{3}\"{4});",
                        field.Key,
                        typeParamStr,
                        sig,
                        field.Key,
                        passedParamString);
                }

                sw.Indent--;
                sw.WriteLine("}");


                sw.Flush();
                context.AddSource("strs.g.cs", stringWriter.ToString());
            }

            // context.AddSource("LocStr.g.cs", LOC_STR_SOURCE);
        }

        private List<TranslationPair> GetFields(string? dataText)
        {
            if (string.IsNullOrWhiteSpace(dataText))
                return new();

            Dictionary<string, string> data;
            try
            {
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();

                data = deserializer.Deserialize<Dictionary<string, string>>(dataText!);
                if (data is null)
                    return new();
            }
            catch (YamlException ye)
            {
                Debug.WriteLine($"YAML parsing error: {ye.Message}");
                return new();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error reading YAML: {ex.Message}");
                return new();
            }

            var list = new List<TranslationPair>();
            foreach (var entry in data)
            {
                list.Add(new TranslationPair(
                    entry.Key,
                    entry.Value
                ));
            }

            return list;
        }
    }
}