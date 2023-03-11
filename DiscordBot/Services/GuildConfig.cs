using Discord;
using Discord.WebSocket;

namespace DiscordBot.Services;

public class GuildConfig
{
    private static readonly List<GuildConfig> GuildMaster = new();

    public GuildConfig(SocketGuild guild)
    {
        Guild = guild;
        AudioService = new AudioService(this);
        GuildMaster.Add(this);
    }

    public GuildTimer Timer { get; } = new();

    public SocketGuild Guild { get; }

    public AudioService AudioService { get; }

    /// <summary>
    ///     The current bots VC, if connected
    /// </summary>
    public IVoiceChannel? BotsVoiceChannel => Guild.GetUser(Globals.Client.CurrentUser.Id)?.VoiceChannel;

    public static GuildConfig Of(IGuild guild)
    {
        return GuildMaster.FirstOrDefault(x => x.Guild.Id == guild.Id) ?? new GuildConfig((SocketGuild)guild);
    }
}