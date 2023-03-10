namespace DiscordBot;

public static class Secrets
{
    public static readonly string? DiscordToken = Environment.GetEnvironmentVariable("DiscordToken");
    public static readonly string? DevGuildId = Environment.GetEnvironmentVariable("DevGuildID");
    public static readonly string? GoogleApiKey = Environment.GetEnvironmentVariable("GoogleApiKey");
}