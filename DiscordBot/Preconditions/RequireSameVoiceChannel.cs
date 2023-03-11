using Discord;
using Discord.Interactions;
using DiscordBot.Services;

namespace DiscordBot.Preconditions;

public class RequireSameVoiceChannel : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context,
        ICommandInfo commandInfo, IServiceProvider services)
    {
        var user = (IGuildUser)context.User;
        var guildConfig = GuildConfig.Of(context.Guild);
        var botsVoiceChannel = guildConfig.BotsVoiceChannel;

        
        if (botsVoiceChannel != null && botsVoiceChannel.Id != user.VoiceChannel?.Id)
            return Task.FromResult(PreconditionResult.FromError(
                $"You must be in the same voice channel ({botsVoiceChannel.Mention}) as the bot to use this command."));
        
        if (user.VoiceChannel is null)
            return Task.FromResult(PreconditionResult.FromError("You must be in a voice channel to use this command"));

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}