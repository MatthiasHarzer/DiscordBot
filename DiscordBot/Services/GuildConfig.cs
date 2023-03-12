using Discord;
using Discord.WebSocket;

namespace DiscordBot.Services;

/// <summary>
/// A config and store for each guild to store session and cross-session data
/// </summary>
public class GuildConfig
{
    private static readonly List<GuildConfig> GuildMaster = new();


    private GuildConfig(SocketGuild guild)
    {
        Guild = guild;
        AudioService = new AudioService(this);
        GuildMaster.Add(this);
    }

    /// <summary>
    /// A timer that can run periodical or one off tasks for this guild
    /// </summary>
    public GuildTimer Timer { get; } = new();

    /// <summary>
    /// The guild this config is for
    /// </summary>
    public SocketGuild Guild { get; }

    /// <summary>
    /// The audio service for this guild
    /// </summary>
    public AudioService AudioService { get; }

    /// <summary>
    /// The current bots VC, if connected
    /// </summary>
    public IVoiceChannel? BotsVoiceChannel => Guild.GetUser(Globals.Client.CurrentUser.Id)?.VoiceChannel;

    /// <summary>
    /// Returns the config for the given guild, or creates a new one if it doesn't exist
    /// </summary>
    /// <param name="guild">The guild the config is for</param>
    /// <returns>The guild config</returns>
    public static GuildConfig Of(IGuild guild)
    {
        return GuildMaster.FirstOrDefault(x => x.Guild.Id == guild.Id) ?? new GuildConfig((SocketGuild)guild);
    }
}