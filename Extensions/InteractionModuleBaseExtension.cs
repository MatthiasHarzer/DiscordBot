using Discord.Interactions;
using DiscordBot.Services;

namespace DiscordBot.Extensions;

public static class InteractionModuleBaseExtension
{
    public static GuildConfig GetGuildConfig(this InteractionModuleBase<SocketInteractionContext> interactionModuleBase)
    {
        return GuildConfig.Of(interactionModuleBase.Context.Guild);
    }
}