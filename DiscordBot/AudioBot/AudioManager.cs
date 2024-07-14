using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordBot.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.AudioBot
{
    internal class AudioManager
    {
        private static AudioManager _instance;
        private static readonly object _lock = new();

        private AudioManager() { }

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AudioManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private IAudioClient _audioClient;
        private CancellationTokenSource _cancellationTokenSource;

        public async Task PlaySong(SocketSlashCommand command)
        {
            await CheckAndJoinVoice(command);

            var urlOption = command.Data.Options.First();
            string videoUrl = urlOption?.Value?.ToString();

            Log($"[{command.User.Username}] Searching {videoUrl}");
            await command.RespondAsync(text: $"Searching: `{videoUrl}`", ephemeral: true);

            string title = await AudioRipper.GetSongTitle(videoUrl);
            await command.FollowupAsync(text: $"Added **{title}** to Queue!", ephemeral: true);
            await command.DeleteOriginalResponseAsync();
            Log($"[{command.User.Username}] Added {title} to Queue");

            if (videoUrl != null)
            {
                if (_cancellationTokenSource == null)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                await AudioRipper.EnqueueYoutubeTask(_audioClient, videoUrl, title, _cancellationTokenSource.Token);
            }
        }

        public async Task PlaySong(string title, string url)
        {
            Log($"Playing Song: {title} {url}");
            if (url != null)
            {
                if (_cancellationTokenSource == null)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                await AudioRipper.EnqueueYoutubeTask(_audioClient, url, title, _cancellationTokenSource.Token);
            }
        }

        public async Task SongQueue(SocketSlashCommand command)
        {
            var songs = AudioRipper.GetSongQueue();
            var result = "";
            for (var i = 0; i < songs.Count; i++)
                result += $"***#{i + 1}*** {songs[i].Title}\n";
            Log($"[{command.User.Username}] Checking song queue");
            await command.RespondAsync(result, ephemeral: true);
        }

        public async Task SkipSong(SocketSlashCommand command)
        {
            Log($"[{command.User.Username}] Skipping song");
            _cancellationTokenSource?.Cancel();
            await command.RespondAsync(text: $"Song skipped.", ephemeral: true);

            // Reset the cancellation token and play the next song
            _cancellationTokenSource = new CancellationTokenSource();
            await AudioRipper.PlayNextSong(_audioClient, _cancellationTokenSource.Token);
        }

        public async Task StopSong(SocketSlashCommand command)
        {
            Log($"[{command.User.Username}] Stopping song");
            _cancellationTokenSource?.Cancel();
            await command.RespondAsync(text: $"Playback stopped.", ephemeral: true);
        }

        public async Task ChangeVolume(SocketSlashCommand command)
        {
            Log($"[{command.User.Username}] Changing volume");
            // Implement volume change logic here

            await command.RespondAsync(text: $"Volume change not implemented yet...", ephemeral: true);
        }

        public async Task<bool> CheckAndJoinVoice(IGuildUser user)
        {
            var voiceChannel = user?.VoiceChannel;

            if (voiceChannel == null)
            {
                Log("Bot failed to join voice");
                return false;
            }

            if (_audioClient == null)
            {
                try
                {
                    Log("Joined voice channel");
                    _audioClient = await voiceChannel.ConnectAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"Failed to connect to the voice channel: {ex.Message}");
                }
            }

            Log("Bot is already in voice.");
            return true;
        }

        public async Task CheckAndJoinVoice(SocketSlashCommand command)
        {
            var user = command.User as IGuildUser;
            var voiceChannel = user?.VoiceChannel;

            if (voiceChannel == null)
            {
                Log("Bot failed to join voice");
                await command.RespondAsync("You need to join a voice channel first.", ephemeral: true);
                return;
            }

            if (_audioClient == null)
            {
                try
                {
                    Log("Joined voice channel");
                    _audioClient = await voiceChannel.ConnectAsync();
                }
                catch (Exception ex)
                {
                    await command.RespondAsync($"Failed to connect to the voice channel: {ex.Message}", ephemeral: true);
                }
            }
        }

        private void Log(string text)
        {
            Console.WriteLine($"[AudioManager] {text}");
        }
    }
}
