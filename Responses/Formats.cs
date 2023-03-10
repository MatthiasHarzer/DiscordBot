using YoutubeDLSharp.Metadata;

namespace DiscordBot.Responses;

public static class Formats
{
    private static string SecondsToTime(int seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        var hours = time.Hours > 0 ? $"{time.Hours:D2}:" : "";
        return $"{hours}{time.Minutes:D2}:{time.Seconds:D2}";
    }

    public static string GetVideoLinked(VideoData videoData)
    {
        return
            $"[`{videoData.Title} - {videoData.Artist ?? videoData.Uploader} ({SecondsToTime((int)(videoData.Duration ?? -1))})`]({videoData.Url})";
    }
}