using System.Diagnostics;
using Discord;
using Discord.Audio;
using DiscordBot.Responses;
using DiscordBot.Utility;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeExplode;
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

    /// <summary>
    /// Indicates whether the bot is currently processing a queue
    /// </summary>
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

    /// <summary>
    /// Plays a song from a provided file path.
    /// </summary>
    /// <param name="file">The file to playback</param>
    /// <param name="voiceChannel">The voicechannel to play the audio in</param>
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

    /// <summary>
    /// Checks if the maximum amount of cached files is reached and deletes the oldest ones if so
    /// </summary>
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

    /// <summary>
    /// Fetches the video ids from a playlist url.
    /// </summary>
    /// <param name="query">The playlist url</param>
    /// <returns>The id's of playlist songs or null, if the provided query is not a playlist url</returns>
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

    /// <summary>
    /// Fetches the video data from a video id
    /// </summary>
    /// <param name="id">The videos ID</param>
    /// <returns>The video data or null, if the provided ID is not valid</returns>
    private async Task<VideoData?> GetVideoFromId(string id)
    {
        var res = await _youtubeDlClient.RunVideoDataFetch(id);
        if (!res.Success) return null;
        res.Data.Url = $"https://www.youtube.com/watch?v={id}";
        return res.Data;
    }

    /// <summary>
    /// Fetches the video ids from a query. Performs a search if the query is not a video or playlist url.
    /// </summary>
    /// <param name="query">The query to search for</param>
    /// <returns>A update of whats going on, an finalises with the id's of the videos</returns>
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

    /// <summary>
    /// Enqueues the given video ids.
    /// This can be a time consuming task due to video data fetching. Therefore its recommended to call this in a seperate thread.
    /// </summary>
    /// <param name="videoIds">The video id's to enqueue</param>
    /// <param name="shuffle">Whether to shuffle the videos or not</param>
    private async Task Enqueue(IEnumerable<string> videoIds, bool shuffle = false)
    {
        List<string> ids = new(videoIds);
        if (shuffle)
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

    /// <summary>
    /// Resolves the given query and plays the first result. Enqueues the rest of the results if the query was a
    /// playlist or the bot is already playing back some audio.
    /// </summary>
    /// <param name="updateWith">The method called, for updating the status of the bots response.</param>
    /// <param name="query">The query to search for</param>
    /// <param name="voiceChannel">The voice channel to play back the audio.</param>
    /// <param name="shuffle">Whether to shuffle the songs, if the given query was a playlist.</param>
    /// <returns></returns>
    private async Task<FormattedMessage> PlayWorker(Func<FormattedMessage, Task> updateWith, string query,
        IVoiceChannel voiceChannel, bool shuffle = false)
    {
        var videoFetcher = GetVideoIdsFromQuery(query);

        videoFetcher.OnUpdate += updateWith;

        var videoIds = await videoFetcher.Result;

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


        var file = await downloader.Result;

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

    /// <summary>
    /// Plays the given query in the given voice channel.
    /// </summary>
    /// <param name="query">The query to play</param>
    /// <param name="voiceChannel">The voice channel to play in</param>
    /// <param name="shuffle">Whether to shuffle the queue, if the query was a playlist</param>
    /// <returns></returns>
    public PartiallyFinishedValue<FormattedMessage, FormattedMessage> Play(string query, IVoiceChannel voiceChannel,
        bool shuffle = false)
    {
        if (Processing)
            return new PartiallyFinishedValue<FormattedMessage, FormattedMessage>(
                AudioModuleResponses.Processing());

        Processing = true;
        var response = new PartiallyFinishedValue<FormattedMessage, FormattedMessage>(
            worker: async (updateWith) => await PlayWorker(updateWith, query, voiceChannel, shuffle));

        response.OnFinished += (_) => Task.FromResult(Processing = false);

        return response;
    }

    /// <summary>
    /// Fetches the video data next in the queue and plays it.
    /// Disconnects the bot from the voice channel if the queue is empty.
    /// </summary>
    private async Task Next()
    {
        if (Queue.Count <= 0)
        {
            await Disconnect();
            return;
        }

        var video = Queue.Dequeue();

        var downloader = await DownloadAudioOrGetCached(video);

        var file = await downloader.Result;

        if (file == null || _guildConfig.BotsVoiceChannel == null) return;

        _ = PlayFromFile(file, _guildConfig.BotsVoiceChannel);
        CurrentSong = video;
    }

    /// <summary>
    /// Checks if the bot is the only user in the given voice channel.
    /// </summary>
    /// <param name="channel">The voiceChannel to check</param>
    /// <returns></returns>
    private async Task<bool> IsAlone(IVoiceChannel channel)
    {
        var users = await channel.GetUsersAsync().FlattenAsync();
        var guildUsers = users as IGuildUser[] ?? users.ToArray();
        return guildUsers.All(user => user.IsBot);
    }

    /// <summary>
    /// Connects the bot to the given voice channel.
    /// </summary>
    /// <param name="channel">The channel to connect to</param>
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

    /// <summary>
    /// Stops the playback and disconnects the bot from the voice channel.
    /// Also clears the queue.
    /// </summary>
    public async Task Disconnect()
    {
        Queue.Clear();
        Playing = false;
        CurrentSong = null;
        if (_client is null) return;
        await _client.StopAsync();
        _client = null;
    }

    /// <summary>
    /// Skips the current song and plays the next one.
    /// </summary>
    /// <returns>The next song</returns>
    public VideoData? Skip()
    {
        var upcoming = Queue.Count > 0 ? Queue.Peek() : null;
        _ffmpegProcess?.Kill();
        return upcoming;
    }

    /// <summary>
    /// Enqueues the given video query at the first position in the queue.
    /// If the query is a playlist, null is returned.
    /// </summary>
    /// <param name="query">The query to search for</param>
    /// <returns>The songs data</returns>
    public async Task<VideoData?> SetNext(string query)
    {
        var videoFetcher = GetVideoIdsFromQuery(query);
        var videoIds = await videoFetcher.Result;

        if (videoIds is null || videoIds.Count != 1) return null;

        var nextVideo = (await GetVideoFromId(videoIds[0]))!;
        var list = Queue.ToList();
        list.Insert(0, nextVideo);
        Queue = new Queue<VideoData>(list);

        return nextVideo;
    }

    /// <summary>
    /// Shuffles the queue.
    /// </summary>
    public void Shuffle()
    {
        var list = Queue.ToList();
        list = list.Shuffle();
        Queue = new Queue<VideoData>(list);
    }
}