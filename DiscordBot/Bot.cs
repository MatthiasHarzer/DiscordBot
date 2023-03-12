using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DiscordBot;

internal class Bot
{
    private readonly DiscordSocketClient _client;
    private InteractionService _interactionService = null!;

    private Bot()
    {
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _client.Ready += ClientReady;
        Globals.Client = _client;
    }


    public static Task Main(string[] args)
    {
        if (Constants.DiscordToken == null)
        {
            Console.WriteLine("Discord Token must not be null!");
            return Task.CompletedTask;
        }
        
        if(Constants.GoogleApiKey == null)
        {
            Console.WriteLine("Google API Key should not be null. Some commands will not work!");
        }

        // Check if the downloads directory exists
        if (!Directory.Exists(Constants.DownloadDir)) Directory.CreateDirectory(Constants.DownloadDir);

        return new Bot().MainAsync();
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task MainAsync()
    {
        await _client.LoginAsync(TokenType.Bot, Constants.DiscordToken);
        await _client.StartAsync();


        _interactionService = new InteractionService(
            _client.Rest,
            Constants.InteractionServiceConfig
        );

        await Task.Delay(-1);
    }

    private async Task RegisterCommands()
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), null);

#if DEBUG
        if (Constants.DevGuildId == null) return;
        var devGuildId = Convert.ToUInt64(Constants.DevGuildId);
        await _interactionService.RegisterCommandsToGuildAsync(devGuildId);
#else
            await _interactionService.RegisterCommandsGloballyAsync();
#endif
        Globals.Commands = _interactionService.SlashCommands;
    }

    private async Task ClientReady()
    {
        await RegisterCommands();
        _interactionService.Log += Log;
        _client.InteractionCreated += HandleInteractionAsync;
        _interactionService.InteractionExecuted += async (info, context, result) =>
        {
            if (result.IsSuccess) return;

            await context.Interaction.RespondAsync(result.ErrorReason, ephemeral: true);
        };
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);
        await _interactionService.ExecuteCommandAsync(context, null);
    }
}