using Discord.Interactions;

namespace DiscordBot;

public static class Constants
{
    public static readonly InteractionServiceConfig InteractionServiceConfig = new()
    {
        DefaultRunMode = RunMode.Async
    };

    public static readonly string DownloadDir = Path.Combine(Environment.CurrentDirectory, "Downloads");
    public static readonly string YoutubeDlpPath = Environment.GetEnvironmentVariable("YoutubeDlpPath") ?? "youtube-dlp";
    public const int MinResponseUpdateDelay = 1500; //ms
    public const int DiscordEmbedFieldLimit = 1024;
}