#nullable disable
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SantiBot.Services;

public class YtdlOperation
{
    private const string COOKIES_PATH = "data/ytcookies.txt";

    private readonly string _baseArgString;

    public YtdlOperation(string baseArgString)
    {
        _baseArgString = baseArgString;
    }

    private Process CreateProcess(string[] args)
    {
        var newArgs = args.Map(arg => (object)arg.Replace("\"", ""));
        var arguments = string.Format(_baseArgString, newArgs);

        // Use node as JS runtime (required for YouTube extraction)
        arguments = "--js-runtimes node " + arguments;

        if (File.Exists(COOKIES_PATH))
            arguments = $"--cookies \"{COOKIES_PATH}\" " + arguments;

        return new()
        {
            StartInfo = new()
            {
                FileName = "yt-dlp",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            }
        };
    }

    public async Task<string> GetDataAsync(params string[] args)
    {
        try
        {
            using var process = CreateProcess(args);

            Log.Debug("Executing {FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);
            process.Start();

            var str = await process.StandardOutput.ReadToEndAsync();
            var err = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(err))
                Log.Warning("yt-dlp warning: {YtdlWarning}", err);

            return str;
        }
        catch (Win32Exception)
        {
            Log.Error("yt-dlp is likely not installed. Please install it before running the command again");
            return default;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception running yt-dlp: {ErrorMessage}", ex.Message);
            return default;
        }
    }

    public async IAsyncEnumerable<string> EnumerateDataAsync(params string[] args)
    {
        using var process = CreateProcess(args);

        Log.Debug("Executing {FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);
        process.Start();

        string line;
        while ((line = await process.StandardOutput.ReadLineAsync()) is not null)
            yield return line;
    }
}