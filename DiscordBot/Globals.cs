using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

namespace DiscordBot;

/// <summary>
///     A place to store all global cross guild data
/// </summary>
public static class Globals
{
    public static DiscordSocketClient Client { get; set; } = null!;
    public static IReadOnlyList<SlashCommandInfo> Commands { get; set; } = null!;
}