# DiscordBot

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A neat little Discord Bot that plays music.

<br>
Built using [DiscordNET](https://discordnet.dev/)

### Todo for own use:
Configure the [`Secrets.cs`](./Secrets.cs) file to your needs with the DiscordToken and Api keys or set the environmnet variables accordingly:
```cs
namespace DiscordBot;

public static class Secrets
{
    public static readonly string DiscordToken = @"YOUR-DISCORD-TOKEN";
    public static readonly string GoogleApiKey = @"YOUR-GOOGLE-CLOUD-API-KEY";
}

```

### Installation notice

Requries [Opus](https://ftp.osuosl.org/pub/xiph/releases/opus/), [Sodium](https://download.libsodium.org/libsodium/releases/) and [FFmpeg](https://ffmpeg.org/) to play audio, aswell as [yt-dlp](https://github.com/yt-dlp/yt-dlp) to fetch the audio stream from YouTube.


