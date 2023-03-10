using System.Diagnostics;
using Discord;
using Discord.Audio;
using DiscordBot.Responses;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;


namespace DiscordBot.Services;

public class AudioService
{
    private readonly GuildConfig _guildConfig;

    private readonly YoutubeDL _youtubeDlClient;

    private IAudioClient? _client;

    private Process? _ffmpegProcess;

    public Queue<VideoData> Queue { get; } = new();

    public bool Playing { get; private set; }

    public bool Processing { get; private set; }

    public VideoData? CurrentVideo { get; private set; }

    public AudioService(GuildConfig guildConfig)
    {
        _guildConfig = guildConfig;
        _youtubeDlClient = new YoutubeDL()
        {
            YoutubeDLPath = Constants.YoutubeDlpPath,
            OutputFolder = Constants.DownloadDir,
        };
    }

    private Process CreateStream(string path)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        })!;
    }

    private async Task PlayFromFile(string file, IVoiceChannel voiceChannel, Action? onFail = null)
    {
        try
        {
            if (_client is null) await Connect(voiceChannel);
        }
        catch (Exception)
        {
            onFail?.Invoke();
            return;
        }


        if (_client is null) return;

        Playing = true;

        // _client.CreateOpusStream()

        _ffmpegProcess = CreateStream(file);
        await using var output = _ffmpegProcess.StandardOutput.BaseStream;
        await using var discord = _client.CreatePCMStream(AudioApplication.Music);
        try
        {
            await output.CopyToAsync(discord);
        }
        finally
        {
            await discord.FlushAsync();
        }

        _ = Next();
    }

    private Task<PartiallyFinishedValue<int, string>> DownloadAudioOrGetCached(VideoData video)
    {
        var fileName = video.ID + ".mp3";
        var outputPath = Path.Combine(Constants.DownloadDir, fileName);
        if (File.Exists(outputPath)) return Task.FromResult(new PartiallyFinishedValue<int, string>(outputPath));


        PartiallyFinishedValue<int, string> partially = new();
        var progress = new Progress<DownloadProgress>(
            p => partially.Update((int)(p.Progress * 100))
        );

        partially.Worker = async (_) =>
        {
            var res = await _youtubeDlClient.RunAudioDownload(
                video.ID,
                progress: progress
            );

            if (!res.Success) return null;

            string path = res.Data;

            File.Move(path, outputPath);

            return outputPath;
        };

        return Task.FromResult(partially);
    }

    private PartiallyFinishedValue<FormattedMessage, VideoData> GetVideoFromQuery(string query)
    {
        var value = new PartiallyFinishedValue<FormattedMessage, VideoData>();

        value.Worker = async (updateWith) =>
        {
            VideoData video;

            var res = await _youtubeDlClient.RunVideoDataFetch(query);

            if (res.Success)
            {
                video = res.Data;
            }
            else
            {
                await updateWith(
                    AudioModuleResponses.SearchingYoutube(query)
                );

                var apiService = YouTubeApiService.Get();
                var results = await apiService.Search(query);

                if (results.Count <= 0) return null;

                video = (await _youtubeDlClient.RunVideoDataFetch(results[0].Id.VideoId)).Data;
            }

            video.Url = $"https://www.youtube.com/watch?v={video.ID}";
            return video;
        };

        return value;
    }

    private async Task<FormattedMessage> PlayWorker(Func<FormattedMessage, Task> updateWith, string query,
        IVoiceChannel voiceChannel)
    {
        var videoFetcher = GetVideoFromQuery(query);

        videoFetcher.OnUpdate += text =>
        {
            updateWith(text);
            return Task.CompletedTask;
        };

        var video = await videoFetcher.Result();

        if (video == null) return AudioModuleResponses.NoResultsFound(query);

        for (int i = 0; i < 100; i++)
        {
            Queue.Enqueue(video);
        }

        if (Playing)
        {
            Queue.Enqueue(video);
            return AudioModuleResponses.AddedToQueue(video, Queue.Count);
        }


        var downloader = await DownloadAudioOrGetCached(video);

        downloader.OnUpdate += percentage =>
        {
            updateWith(AudioModuleResponses.DownloadingVideo(video, percentage));
            return Task.CompletedTask;
        };

        var file = await downloader.Result();

        if (file == null) return AudioModuleResponses.ErrorDownloadingVideo(video.Title);

    
        
        _ = PlayFromFile(file, voiceChannel, () =>
        {
            updateWith(AudioModuleResponses.UnableToStartPlayback());
        });

        CurrentVideo = video;

        return AudioModuleResponses.PlayingVideo(video, voiceChannel);
    }

    public PartiallyFinishedValue<FormattedMessage, FormattedMessage> Play(string query, IVoiceChannel voiceChannel)
    {
        if (Processing)
            return new PartiallyFinishedValue<FormattedMessage, FormattedMessage>(
                AudioModuleResponses.Processing());

        Processing = true;
        var response = new PartiallyFinishedValue<FormattedMessage, FormattedMessage>();

        response.Worker = async (updateWith) =>
        {
            var res = await PlayWorker(updateWith, query, voiceChannel);
            return res;
        };

        response.Finished += (_) => Task.FromResult(Processing = false);

        return response;
    }

    public async Task Next()
    {
        if (Queue.Count <= 0)
        {
            await Disconnect();
            return;
        }

        var video = Queue.Dequeue();

        var downloader = await DownloadAudioOrGetCached(video);

        var file = await downloader.Result();

        if (file == null || _guildConfig.BotsVoiceChannel == null) return;

        _ = PlayFromFile(file, _guildConfig.BotsVoiceChannel);
        CurrentVideo = video;
    }


    private async Task<bool> IsAlone(IVoiceChannel channel)
    {
        var users = await channel.GetUsersAsync().FlattenAsync();
        var guildUsers = users as IGuildUser[] ?? users.ToArray();
        return guildUsers.All(user => user.IsBot);
    }

    private async Task Connect(IVoiceChannel channel)
    {
        _client = await channel.ConnectAsync();

        _client.ClientDisconnected += async _ =>
        {
            if (await IsAlone(channel)) await Disconnect();
        };

        _client.Disconnected += async _ =>
        {
            Playing = false;
            await Disconnect();
            _ffmpegProcess?.Kill();
        };
    }

    public async Task Disconnect()
    {
        Queue.Clear();
        Playing = false;
        CurrentVideo = null;
        if (_client is null) return;
        await _client.StopAsync();
        _client = null;
    }

    public VideoData? Skip()
    {
        VideoData? upcoming = Queue.Count > 0 ? Queue.Peek() : null;
        _ffmpegProcess?.Kill();
        return upcoming;
    }
}