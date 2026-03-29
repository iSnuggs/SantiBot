#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Introductions")]
    [Group("intro")]
    public partial class IntroCommands : SantiModule<IntroService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task IntroSetup(ITextChannel channel)
        {
            await _service.SetupAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm($"Introductions enabled! New members will be prompted and intros posted to {channel.Mention}.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task IntroTemplate([Leftover] string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                await Response().Error("Please provide intro questions!").SendAsync();
                return;
            }

            await _service.SetTemplateAsync(ctx.Guild.Id, template);
            await Response().Confirm("Intro template updated!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task IntroDisable()
        {
            await _service.DisableAsync(ctx.Guild.Id);
            await Response().Confirm("Introductions disabled.").SendAsync();
        }
    }
}
