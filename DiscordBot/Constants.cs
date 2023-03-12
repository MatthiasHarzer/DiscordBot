using Discord.Interactions;

namespace DiscordBot;

/// <summary>
/// A place to store configurations, constants, and other static data
/// </summary>
public static class Constants
{
    /// <summary>
    /// The configuration for the interaction service
    /// </summary>
    public static readonly InteractionServiceConfig InteractionServiceConfig = new()
    {
        DefaultRunMode = RunMode.Async
    };

    /// <summary>
    /// The minimum delay between response updates (to avoid rate limiting)
    /// </summary>
    public const int MinResponseUpdateDelay = 1500; //ms
    
    /// <summary>
    /// The maximum number of characters allowed in a Discord embed field
    /// </summary>
    public const int DiscordEmbedFieldCharacterLimit = 1024;

    /// <summary>
    /// The maximum number of audio files to cache
    /// </summary>
    public const int MaxCachedFiles = 500;

    /// <summary>
    /// The directory to download files to
    /// </summary>
    public static readonly string DownloadDir = Path.Combine(Environment.CurrentDirectory, "Downloads");
    
    /// <summary>
    /// The binary path for youtube-dlp
    /// </summary>
    public static readonly string YoutubeDlpPath = Environment.GetEnvironmentVariable("YoutubeDlpPath") ?? "yt-dlp";
    
    /// <summary>
    /// The discord bot token
    /// </summary>
    public static readonly string? DiscordToken = Environment.GetEnvironmentVariable("DiscordToken");
    
    /// <summary>
    /// The ID of the development guild. Used for testing.
    /// </summary>
    public static readonly string? DevGuildId = Environment.GetEnvironmentVariable("DevGuildID");
    
    /// <summary>
    /// The API key used for the Youtube API.
    /// </summary>
    public static readonly string? GoogleApiKey = Environment.GetEnvironmentVariable("GoogleApiKey");
}
