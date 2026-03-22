#nullable disable
using SantiBot.Common.TypeReaders;
using static SantiBot.Common.TypeReaders.TypeReaderResult;

namespace SantiBot.Modules.Permissions;

public class CleverbotResponseCmdCdTypeReader : SantiTypeReader<CleverBotResponseStr>
{
    public override ValueTask<TypeReaderResult<CleverBotResponseStr>> ReadAsync(
        ICommandContext ctx,
        string input)
        => input.ToLowerInvariant() == CleverBotResponseStr.CLEVERBOT_RESPONSE
            ? new(FromSuccess(new CleverBotResponseStr()))
            : new(FromError<CleverBotResponseStr>(CommandError.ParseFailed, "Not a valid cleverbot"));
}