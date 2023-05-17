using Discord;
using DiscordBot.Services;
using DiscordBot.Utility;
using YoutubeDLSharp.Metadata;

namespace DiscordBot.Responses;

public static class AudioModuleResponses
{
    public const string QueueProcessingHint = " - List might be incomplete due to processing playlist songs";

    private static EmbedBuilder Template => new()
    {
        Color = Util.RandomColor(),
        Timestamp = DateTimeOffset.Now
    };


    public static FormattedMessage SearchingYoutube(string query)
    {
        return new FormattedMessage(Template
            .AddField($"Searching for `{query}` on YouTube", "This may take a moment"));
    }

    public static FormattedMessage DownloadingVideo(VideoData videoData, int progress)
    {
        return new FormattedMessage(Template
            .AddField("Downloading audio", $"{Formats.GetVideoLinked(videoData)}\n\nProgress: {progress}%"));
    }


    public static FormattedMessage AddedToQueue(VideoData videoData, int newlyAdded, int queueSize)
    {
        var description = $"And {newlyAdded} more enqueued";
        var queueSizeDescription = $"Queue size: {queueSize}";
        return new FormattedMessage(Template
            .AddField("Song added to the queue",
                $"{Formats.GetVideoLinked(videoData)}" +
                "\n\n" + (newlyAdded > 0 ? description : queueSizeDescription)));
    }

    public static FormattedMessage PlayingVideo(VideoData videoData, int additionalAdded, IVoiceChannel channel)
    {
        var description = $"And {additionalAdded} more added to the queue";
        return new FormattedMessage(Template
            .AddField("Now Playing", $"{Formats.GetVideoLinked(videoData)}" +
                                     (additionalAdded > 0 ? $"\n\n{description}" : "") +
                                     $"\n\nJoin {channel.Mention} to listen")
            .WithThumbnailUrl(videoData.Thumbnail));
    }

    public static FormattedMessage NoResultsFound(string? query = null)
    {
        return new FormattedMessage(Template
            .WithDescription(query != null ? $"No results found for `{query}`" : "No results found"));
    }

    public static FormattedMessage Processing()
    {
        return new FormattedMessage(Template
            .WithDescription("Already processing a request. Please wait..."));
    }

    public static FormattedMessage NotInVoiceChannel()
    {
        return new FormattedMessage(Template
            .WithDescription("You must be in a voice channel to use this command"));
    }

    public static FormattedMessage ErrorDownloadingVideo(string title)
    {
        return new FormattedMessage(Template
            .AddField($"Error downloading `{title}`", "Please try again later"));
    }

    public static FormattedMessage SongSkipped(VideoData? upcomingSong)
    {
        return new FormattedMessage(Template
            .AddField("Song skipped", upcomingSong == null
                ? "The queue is empty"
                : $"Now playing: \n{Formats.GetVideoLinked(upcomingSong)}"));
    }

    public static FormattedMessage QueueCleared()
    {
        return new FormattedMessage(Template
            .WithDescription("Queue cleared"));
    }


    public static FormattedMessage QueuePage(int page, List<string> pages, AudioService service)
    {
        var embed = Template;
        if (service.CurrentSong is not null)
            embed.AddField("Currently playing", Formats.GetVideoLinked(service.CurrentSong));

        if (pages.Count == 0) return new FormattedMessage(embed.AddField("Queue is empty", "Nothing to show."));

        var footer = $"Page {page + 1}/{pages.Count}";

        if (service.ProcessingQueue) footer += QueueProcessingHint;

        return new FormattedMessage(embed
            .AddField($"Queue ({service.Queue.Count})", pages[page])
            .WithFooter(footer));
    }

    public static FormattedMessage NoNextSong()
    {
        return new FormattedMessage(Template
            .WithDescription("There is no upcoming song"));
    }

    public static FormattedMessage Disconnecting()
    {
        return new FormattedMessage(Template
            .WithDescription("Disconnecting..."));
    }


    public static FormattedMessage UnableToStartPlayback()
    {
        return new FormattedMessage(Template
            .WithDescription("Unable to start playback. Please try again later"));
    }

    public static FormattedMessage NextSongQueued(VideoData videoData)
    {
        return new FormattedMessage(Template
            .AddField("Song will play next", Formats.GetVideoLinked(videoData)));
    }

    public static FormattedMessage QueueShuffled(int count)
    {
        return new FormattedMessage(Template
            .WithDescription($"Shuffled {count} songs in the queue"));
    }

    public static FormattedMessage AutoplayIsCurrently(bool enabled)
    {
        return new FormattedMessage(Template
            .WithDescription($"Autoplay is currently `{(enabled ? "enabled" : "disabled")}`"));
    }

    public static FormattedMessage AutoplayToggled(bool enabled)
    {
        return new FormattedMessage(Template
            .WithDescription($"Autoplay has been turned `{(enabled ? "on" : "off")}`"));
    }

    public static FormattedMessage ProcessingTimeout()
    {
        return new FormattedMessage(Template
            .WithDescription("Processing timed out. Please try again later"));
    }
}