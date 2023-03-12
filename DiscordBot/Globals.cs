using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

namespace DiscordBot;

/// <summary>
/// A place to store all global cross guild data
/// </summary>
public static class Globals
{
    /// <summary>
    /// The discord client
    /// </summary>
    public static DiscordSocketClient Client { get; set; } = null!;

    /// <summary>
    /// The slash commands registered with the interaction service
    /// </summary>
    public static IReadOnlyList<SlashCommandInfo> Commands { get; set; } = null!;
}