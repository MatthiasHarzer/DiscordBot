using System.Text.Json.Nodes;
using Discord;
using Discord.WebSocket;
using System.Text.Json;
using System.Text.Json.Nodes;

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
        Import();
    }

    private string FilePath => $"{Constants.GuildConfigDir}/{Guild.Id}.json";

    /// <summary>
    /// If set to true, recommended songs will auto play when the queue is over
    /// </summary>
    private bool _autoPlayEnabled = false;

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
    /// Whether autoplay is enabled
    /// </summary>
    public bool AutoPlay
    {
        get => _autoPlayEnabled;
        set
        {
            _autoPlayEnabled = value;
            Export();
        }
    }

    /// <summary>
    /// Returns the config for the given guild, or creates a new one if it doesn't exist
    /// </summary>
    /// <param name="guild">The guild the config is for</param>
    /// <returns>The guild config</returns>
    public static GuildConfig Of(IGuild guild)
    {
        return GuildMaster.FirstOrDefault(x => x.Guild.Id == guild.Id) ?? new GuildConfig((SocketGuild)guild);
    }

    /// <summary>
    /// Save guild config as file to preserve command states between restarts
    /// </summary>
    private void Export()
    {
        var jsonObject = new JsonObject
        {
            { "guildId", Guild.Id },
            { "autoPlay", AutoPlay },
        };

        File.WriteAllText(FilePath, JsonSerializer.Serialize(jsonObject));
    }

    /// <summary>
    /// Imports guildconfig from a json file
    /// </summary>
    private void Import()
    {
        try
        {
            var content = File.ReadAllText(FilePath);

            var json = JsonSerializer.Deserialize<JsonObject>(content);

            if (json == null || !json.TryGetPropertyValue("guildId", out JsonNode? id)) return;
            if (id == null || (ulong)id != Guild.Id) return;

            if (json.TryGetPropertyValue("autoPlay", out JsonNode? ap))
            {
                if (ap != null) _autoPlayEnabled = (bool)ap;
            }
        }

        catch (Exception)
        {
            // Console.WriteLine(e.Message);
            // Console.WriteLine(e.StackTrace);
            // -> something's fishy with the file or json
        }
    }
}