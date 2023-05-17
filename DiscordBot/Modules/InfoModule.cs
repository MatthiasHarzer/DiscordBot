using Discord;
using Discord.Interactions;
using DiscordBot.Responses;
using DiscordBot.Utility;

namespace DiscordBot.Modules;

public class InfoModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Shows a list of all commands")]
    public async Task HelpAsync()
    {
        await DeferAsync();
        var commands = Globals.Commands;

        var guildCommands = await Context.Guild.GetApplicationCommandsAsync();

        var embed = new EmbedBuilder
            {
                Color = Util.RandomColor(),
                Timestamp = DateTimeOffset.Now
            }
            .WithTitle("Here's a list of commands and their description:");

        foreach (var command in commands)
        {
            var description = command.Description ?? "No description provided";

            var commandId = guildCommands
                .FirstOrDefault(x =>
                    x!.Name == command.Name && x.ApplicationId == Context.Client.CurrentUser.Id, null)?.Id;

            embed.AddField(Formats.GetFormattedCommand(command, commandId), description);
        }

        await ModifyOriginalResponseAsync(properties => properties.Embed = embed.Build());
    }
}