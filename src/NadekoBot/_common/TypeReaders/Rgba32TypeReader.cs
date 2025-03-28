using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;

#nullable disable
namespace NadekoBot.Common.TypeReaders;

public sealed class Rgba32TypeReader : NadekoTypeReader<Rgba32>
{
    public override ValueTask<TypeReaderResult<Rgba32>> ReadAsync(ICommandContext context, string input)
    {
        if (!Color.TryParse(input, out var color))
        {
            Log.Information("Fail");
            return ValueTask.FromResult(
                TypeReaderResult.FromError<Rgba32>(CommandError.ParseFailed, "Parameter is not a valid color hex."));
        }
        Log.Information(color.ToHex());

        return ValueTask.FromResult(TypeReaderResult.FromSuccess((Rgba32)color));

        if (Rgba32.TryParseHex(input, out var clr))
        {
            return ValueTask.FromResult(TypeReaderResult.FromSuccess(clr));
        }

        if (!Enum.TryParse<Color>(input, true, out var clrName))
            return ValueTask.FromResult(
                TypeReaderResult.FromError<Rgba32>(CommandError.ParseFailed,
                    "Parameter is not a valid color hex."));

        Log.Information(clrName.ToString());

        if (Rgba32.TryParseHex(clrName.ToHex(), out clr))
        {
            return ValueTask.FromResult(TypeReaderResult.FromSuccess(clr));
        }

        return ValueTask.FromResult(
            TypeReaderResult.FromError<Rgba32>(CommandError.ParseFailed, "Parameter is not a valid color hex."));
    }
}