using Discord.WebSocket;

namespace DiscordBot;

/// <summary>
///     A place to store all global cross guild data
/// </summary>
public static class Globals
{
    public static DiscordSocketClient Client { get; set; } = null!;
}