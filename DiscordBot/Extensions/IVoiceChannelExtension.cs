using Discord;

namespace DiscordBot.Extensions;

public static class IVoiceChannelExtension
{
    /// <summary>
    ///     Returns all users connected to the voice channel
    /// </summary>
    /// <param name="voiceChannel">The voice channel</param>
    /// <param name="excludeBots">Whether to exclude bots when fetching connected users</param>
    /// <returns>A list of users currently connected to this voice channel</returns>
    public static async Task<List<IGuildUser>> GetConnectedUsers(this IVoiceChannel voiceChannel,
        bool excludeBots = true)
    {
        var userCanViewChannel = (await voiceChannel.GetUsersAsync().ToListAsync().AsTask()).First().ToList();
        var usersConnected = userCanViewChannel.FindAll(u => u.VoiceChannel == voiceChannel);
        return !excludeBots ? usersConnected : usersConnected.FindAll(u => !u.IsBot);
    }
}