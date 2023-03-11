using System.Diagnostics;
using Discord;
using Discord.Audio;
using DiscordBot.Responses;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;


namespace DiscordBot.Services;

/// <summary>
/// Responsible for playing audio
/// </summary>
public class AudioService
{
    private readonly GuildConfig _guildConfig;

    private readonly YoutubeDL _youtubeDlClient;

    private IAudioClient? _client;

    private Process? _ffmpegProcess;

    /// <summary>
    /// The upcoming songs
    /// </summary>
    public Queue<VideoData> Queue { get; private set; } = new();

    /// <summary>
    /// Whether the bot is currently playing a song
    /// </summary>
    public bool Playing { get; private set; }

    /// <summary>
    /// Whether the bot is currently processing a song
    /// </summary>
    private bool Processing { get; set; }


    public bool ProcessingQueue { get; private set; }

    /// <summary>
    /// The current song
    /// </summary>
    public VideoData? CurrentSong { get; private set; }

    /// <summary>
    /// Creates a new instance of <see cref="AudioService"/>
    /// </summary>
    /// <param name="guildConfig">The guild config the audio service is for</param>
    public AudioService(GuildConfig guildConfig)
    {
        _guildConfig = guildConfig;
        _youtubeDlClient = new YoutubeDL()
        {
            YoutubeDLPath = Constants.YoutubeDlpPath,
            OutputFolder = Constants.DownloadDir,
        };
    }

    /// <summary>
    /// Creates a process that streams audio from a file
    /// </summary>
    /// <param name="path">The path of the audio file</param>
    /// <returns>The process</returns>
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

    private async Task PlayFromFile(string file, IVoiceChannel voiceChannel)
    {
        if (_client is null) await Connect(voiceChannel);
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

    private void CheckMaxCachedFiles()
    {
        DirectoryInfo dir = new(Constants.DownloadDir);
        var files = dir.GetFiles("*.mp3").OrderBy(p => p.CreationTime).ToList();
        
        if (files.Count > Constants.MaxCachedFiles)
        {
            var toDelete = files.Take(files.Count - Constants.MaxCachedFiles);
            foreach (var file in toDelete)
            {
                file.Delete();
            }
        }
    }
    
    /// <summary>
    /// Downloads the given video to a file or gets it from the cache
    /// </summary>
    /// <param name="video">The video to download</param>
    /// <returns>onUpdate: the download percentage.<br />onFinish: the file location</returns>
    private Task<PartiallyFinishedValue<int, string>> DownloadAudioOrGetCached(VideoData video)
    {
        CheckMaxCachedFiles();
        
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


    private async Task<List<string>?> GetPlayListIdsFromQuery(string query)
    {
        List<PlaylistVideo> videos;
        try
        {
            videos = await new YoutubeClient().Playlists.GetVideosAsync(query).ToListAsync();
        }
        catch (ArgumentException)
        {
            return null; // Is not a playlist
        }

        return videos.Select(v => v.Id.Value).ToList();
    }

    private async Task<VideoData?> GetVideoFromId(string id)
    {
        var res = await _youtubeDlClient.RunVideoDataFetch(id);
        if (res.Success)
        {
            res.Data.Url = $"https://www.youtube.com/watch?v={id}";
            return res.Data;
        }


        return null;
    }

    private PartiallyFinishedValue<FormattedMessage, List<string>> GetVideoIdsFromQuery(string query)
    {
        var value = new PartiallyFinishedValue<FormattedMessage, List<string>>
        {
            Worker = async updateWith =>
            {
                var videos = await GetPlayListIdsFromQuery(query);

                if (videos is not null) return videos; // Return playlist

                // Is not a playlist
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

                // video.Url = $"https://www.youtube.com/watch?v={video.ID}";
                return new List<string> { video.ID };
            }
        };

        return value;
    }

    private async Task Enqueue(List<string> videoIds, bool shuffle = false)
    {
        List<string> ids = new(videoIds);
        if(shuffle)
            ids = ids.Shuffle();
        ProcessingQueue = true;
        foreach (var id in ids)
        {
            
            var video = await GetVideoFromId(id);
            if (video is null) continue;
            Queue.Enqueue(video);
        }
        
        ProcessingQueue = false;
    }

    private async Task<FormattedMessage> PlayWorker(Func<FormattedMessage, Task> updateWith, string query,
        IVoiceChannel voiceChannel, bool shuffle = false)
    {
        var videoFetcher = GetVideoIdsFromQuery(query);

        videoFetcher.OnUpdate += updateWith;

        var videoIds = await videoFetcher.Result();

        if (videoIds is not { Count: > 0 }) return AudioModuleResponses.NoResultsFound(query);

        var nextVideo = (await GetVideoFromId(videoIds[0]))!;

        if (Playing)
        {
            _ = Enqueue(videoIds, shuffle);
            return AudioModuleResponses.AddedToQueue(nextVideo, videoIds.Count - 1, Queue.Count);
        }

        if (videoIds.Count > 1)
        {
            _ = Enqueue(videoIds.Skip(1).ToList());
        }


        var downloader = await DownloadAudioOrGetCached(nextVideo);

        downloader.OnUpdate += async percentage =>
            await updateWith(AudioModuleResponses.DownloadingVideo(nextVideo, percentage));


        var file = await downloader.Result();

        if (file == null) return AudioModuleResponses.ErrorDownloadingVideo(nextVideo.Title);


        if (_client is null)
        {
            try
            {
                await Connect(voiceChannel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return AudioModuleResponses.UnableToStartPlayback();
            }
        }

        _ = PlayFromFile(file, voiceChannel);

        CurrentSong = nextVideo;

        return AudioModuleResponses.PlayingVideo(nextVideo, videoIds.Count - 1, voiceChannel);
    }

    public PartiallyFinishedValue<FormattedMessage, FormattedMessage> Play(string query, IVoiceChannel voiceChannel, bool shuffle = false)
    {
        if (Processing)
            return new PartiallyFinishedValue<FormattedMessage, FormattedMessage>(
                AudioModuleResponses.Processing());

        Processing = true;
        var response = new PartiallyFinishedValue<FormattedMessage, FormattedMessage>
        {
            Worker = async (updateWith) =>
            {
                var res = await PlayWorker(updateWith, query, voiceChannel, shuffle);
                return res;
            }
        };

        response.Finished += (_) => Task.FromResult(Processing = false);

        return response;
    }

    private async Task Next()
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
        CurrentSong = video;
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
        CurrentSong = null;
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

    public async Task<VideoData?> SetNext(string query)
    {
        var videoFetcher = GetVideoIdsFromQuery(query);
        var videoIds = await videoFetcher.Result();
        
        if(videoIds is null || videoIds.Count != 1) return null;
        
        var nextVideo = (await GetVideoFromId(videoIds[0]))!;
        var list = Queue.ToList();
        list.Insert(0, nextVideo);
        Queue = new Queue<VideoData>(list);

        return nextVideo;
    }
    
    public void Shuffle()
    {
        var list = Queue.ToList();
        list = list.Shuffle();
        Queue = new Queue<VideoData>(list);
    }
}