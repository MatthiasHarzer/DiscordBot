using Discord;
using YoutubeDLSharp.Metadata;

namespace DiscordBot.Responses;

public static class AudioModuleResponses
{
    private static EmbedBuilder Template => new()
    {
        Color = Util.RandomColor(),
        Timestamp = DateTimeOffset.Now
    };


    public static FormattedMessage SearchingYoutube(string query) => new(Template
        .AddField($"Searching for `{query}` on YouTube", "This may take a moment"));

    public static FormattedMessage DownloadingVideo(VideoData videoData, int progress) => new(Template
        .AddField($"Downloading audio", $"{Formats.GetVideoLinked(videoData)}\n\nProgress: {progress}%"));


    public static FormattedMessage AddedToQueue(VideoData videoData, int queueSize) => new(Template
        .AddField($"Song added to the queue", $"{Formats.GetVideoLinked(videoData)}\n\nQueue size: {queueSize}"));

    public static FormattedMessage PlayingVideo(VideoData videoData, IVoiceChannel channel) => new(Template
        .AddField("Now Playing", $"{Formats.GetVideoLinked(videoData)}\n\nJoin {channel.Mention} to listen")
        .WithThumbnailUrl(videoData.Thumbnail));

    public static FormattedMessage NoResultsFound(string query) => new(Template
        .WithDescription($"No results found for `{query}`"));

    public static FormattedMessage Processing() => new(Template
        .WithDescription("Already processing a request. Please wait..."));

    public static FormattedMessage NotInVoiceChannel() => new(Template
        .WithDescription("You must be in a voice channel to use this command"));

    public static FormattedMessage ErrorDownloadingVideo(string title) => new(Template
        .AddField($"Error downloading `{title}`", "Please try again later"));

    public static FormattedMessage SongSkipped(VideoData? upcomingSong) => new(Template
        .AddField("Song skipped", upcomingSong == null
            ? "The queue is empty"
            : $"Now playing {Formats.GetVideoLinked(upcomingSong)}"));

    public static FormattedMessage QueueCleared() => new(Template
        .WithDescription("Queue cleared"));


    public static FormattedMessage QueuePage(int page, List<string> pages, int queueCount, VideoData? currentSong)
    {
        var embed = Template;
        if (currentSong is not null)
        {
            embed.AddField("Currently playing", Formats.GetVideoLinked(currentSong));
        }

        if (pages.Count == 0)
        {
            return new(embed.AddField("Queue is empty", "Nothing to show."));
        }

        return new(embed
            .AddField($"Queue ({queueCount})", pages[page])
            .WithFooter($"Page {page + 1}/{pages.Count}"));
    }
    
    public static FormattedMessage Disconnecting() => new(Template
        .WithDescription("Disconnecting..."));
    
    
    public static FormattedMessage UnableToStartPlayback() => new(Template
        .WithDescription("Unable to start playback. Please try again later"));
}
