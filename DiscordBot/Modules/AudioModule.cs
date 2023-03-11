using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord;
using Discord.Interactions;
using DiscordBot.Extensions;
using DiscordBot.Preconditions;
using DiscordBot.Responses;

namespace DiscordBot.Modules;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class AudioModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string QueueButtonIntend = "previous";

    private long _lastUpdate;

    private FormattedMessage _newContent = null!;
    private Color? _oldColor;

    private bool _isUpdating;
    private bool _isProcessing;

    private async Task EditOrFollowUpAsync(FormattedMessage message)
    {
        _newContent = message;

        if (_oldColor is not null && _newContent.Embed is not null)
            _newContent.Embed.WithColor(_oldColor.Value);

        if (_isUpdating) return;
        _isUpdating = true;

        _oldColor = message.Embed?.Color;

        async Task Update()
        {
            _isUpdating = false;
            _isProcessing = true;

            var original = await Context.Interaction.GetOriginalResponseAsync();

            if (original is not null)
                await original.ModifyAsync(properties =>
                {
                    properties.Embed = _newContent.BuiltEmbed;
                    properties.Components = _newContent.Components?.Build();
                });
            else
                await RespondAsync(
                    embed: _newContent.BuiltEmbed,
                    components: _newContent.Components?.Build()
                );

            _isProcessing = false;
        }


        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var diff = now - _lastUpdate;

        if (diff < Constants.MinResponseUpdateDelay)
        {
            await Task.Delay(Constants.MinResponseUpdateDelay - (int)diff);
        }

        while (_isProcessing)
            await Task.Delay(100);

        _lastUpdate = now;
        await Update();
    }

    private List<string> GetQueuePages()
    {
        List<string> pages = new();

        var queue = this.GetGuildConfig().AudioService.Queue;
        int characterCount = 0;

        StringBuilder sb = new();

        for (int i = 0; i < queue.Count; i++)
        {
            var video = queue.ToArray()[i];

            var line = $"\n\n{i + 1}. {Formats.GetVideoLinked(video)}";

            if (characterCount + line.Length > Constants.DiscordEmbedFieldLimit)
            {
                pages.Add(sb.ToString());
                sb.Clear();
                characterCount = 0;
            }

            sb.Append(line);
            characterCount += line.Length;
        }

        if (sb.Length > 0)
        {
            pages.Add(sb.ToString());
        }


        return pages;
    }

    [RequireSameVoiceChannel]
    [SlashCommand("play", "Plays the given song in the current voice-channel")]
    public async Task Play(string query, bool shuffle = false)
    {
        await DeferAsync();
        var guild = this.GetGuildConfig();

        var audioService = guild.AudioService;

        var channel = (Context.User as IGuildUser)?.VoiceChannel;

        if (channel == null)
        {
            await EditOrFollowUpAsync(AudioModuleResponses.NotInVoiceChannel());
            return;
        }


        try
        {
            var response = audioService.Play(query, channel, shuffle);

            response.OnUpdate += async res => { await EditOrFollowUpAsync(res); };

            var result = await response.Result();

            if (result == null)
                await EditOrFollowUpAsync(
                    new FormattedMessage(new EmbedBuilder().WithDescription("Something went wrong")));

            result!.Embed?.WithFooter($"Requested by {Context.User.Username}");

            await EditOrFollowUpAsync(result);
        }
        catch (Exception)
        {
            await EditOrFollowUpAsync(
                new FormattedMessage(new EmbedBuilder().WithDescription("Unable to start playback.")));
        }
    }

    [MessageCommand("play")]
    public async Task PlayMessage(IMessage message)
    {
        if (message.Content.Length <= 0)
        {
            await RespondAsync("Invalid message", ephemeral: true);
            return;
        }

        await Play(message.Content);
    }

    [RequireSameVoiceChannel]
    [SlashCommand("stop", "Stops the current song")]
    public async Task Stop()
    {
        await DeferAsync();
        var guild = this.GetGuildConfig();

        var audioService = guild.AudioService;

        await audioService.Disconnect();

        await EditOrFollowUpAsync(AudioModuleResponses.Disconnecting());
    }

    [RequireSameVoiceChannel]
    [SlashCommand("skip", "Skips the current song")]
    public async Task Skip()
    {
        await DeferAsync();
        var guild = this.GetGuildConfig();

        var audioService = guild.AudioService;

        var upcoming = audioService.Skip();

        await EditOrFollowUpAsync(
            AudioModuleResponses.SongSkipped(upcoming)
        );
    }

    [RequireSameVoiceChannel]
    [SlashCommand("clear", "Clears the current queue")]
    public async Task Clear()
    {
        await DeferAsync();
        var guild = this.GetGuildConfig();

        var audioService = guild.AudioService;

        audioService.Queue.Clear();

        await EditOrFollowUpAsync(AudioModuleResponses.QueueCleared());
    }

    private async Task HandleQueue(int page = 0)
    {
        var pages = GetQueuePages();
        var guild = this.GetGuildConfig();

        var response = AudioModuleResponses.QueuePage(page, pages, guild.AudioService);

        if (pages.Count > 0)
        {
            response.WithComponents(new ComponentBuilder()
                .WithButton("⬅️", QueueButtonIntend + ":" + (page - 1), ButtonStyle.Secondary, disabled: page == 0)
                .WithButton("➡️", QueueButtonIntend + ":" + (page + 1), ButtonStyle.Secondary,
                    disabled: pages.Count == page + 1)
            );
        }


        await EditOrFollowUpAsync(response);
    }

    [SlashCommand("queue", "Shows the current queue")]
    public async Task Queue()
    {
        await DeferAsync();
        try
        {
            await HandleQueue();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    [ComponentInteraction(QueueButtonIntend + ":*")]
    public async Task JumpToPage(int page)
    {
        await DeferAsync();

        await HandleQueue(page);
    }

    [SlashCommand("next", "Shows or sets the next song")]
    public async Task Next(string? query = null)
    {
        await DeferAsync();
        var guild = this.GetGuildConfig();

        var audioService = guild.AudioService;

        if (query == null)
        {
            if (audioService.Queue.Count == 0)
            {
                await EditOrFollowUpAsync(AudioModuleResponses.NoNextSong());
            }

            await HandleQueue();
            return;
        }

        var response = await audioService.SetNext(query);


        if (response == null)
            await EditOrFollowUpAsync(AudioModuleResponses.NoResultsFound());

        var message = AudioModuleResponses.NextSongQueued(response!);
        message.Embed?.WithFooter($"Requested by {Context.User.Username}");

        await EditOrFollowUpAsync(message);
    }
    
    [SlashCommand("shuffle", "Shuffles the current queue")]
    public async Task Shuffle()
    {
        await DeferAsync();
        var guild = this.GetGuildConfig();

        var audioService = guild.AudioService;

        audioService.Shuffle();

        await EditOrFollowUpAsync(AudioModuleResponses.QueueShuffled(audioService.Queue.Count));
    }
}