#nullable disable
using CommandLine;

namespace SantiBot.Common;

public class LbOpts : ISantiCommandOptions
{
    [Option('c', "clean", Default = false, HelpText = "Only show users who are on the server.")]
    public bool Clean { get; set; }

    public void NormalizeOptions()
    {
    }
}