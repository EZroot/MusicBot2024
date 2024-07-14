using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.AudioBot
{
    internal class AudioManager
    {
        // The static instance of the singleton
        private static AudioManager _instance;

        // Lock object for thread safety
        private static readonly object _lock = new();

        // Private constructor to prevent instantiation
        private AudioManager()
        {
            // Initialize any necessary resources here
        }

        // Public method to access the singleton instance
        public static AudioManager Instance
        {
            get
            {
                // Double-check locking for thread safety
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

        public async Task PlaySong(SocketSlashCommand command)
        {
            await CheckAndJoinVoice(command);

            var urlOption = command.Data.Options.First();
            string? videoUrl = urlOption?.Value?.ToString();

            Log($"[{command.User.Username}] Searching {videoUrl}");
            await command.RespondAsync(text: $"Searching: `{videoUrl}`", ephemeral: true);

            string title = "test";//await GetSongTitle(videoUrl);
            await command.FollowupAsync(text: $"Added **{title}** to Queue!", ephemeral: true);
            await command.DeleteOriginalResponseAsync();
            Log($"[{command.User.Username}] Playing {title}");
            if (videoUrl != null) await AudioRipper.EnqueueYoutubeTask(_audioClient, videoUrl, title);
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
            await command.RespondAsync(text: $"Skip song not implemented yet...", ephemeral: true);
        }

        public async Task StopSong(SocketSlashCommand command)
        {
            Log($"[{command.User.Username}] Stopping song");
            await command.RespondAsync(text: $"Stop song not implemented yet...", ephemeral: true);
        }

        public async Task ChangeVolume(SocketSlashCommand command)
        {
            Log($"[{command.User.Username}] Changing volume");
            //var volume = (double)command.Data.Options.First();
            await command.RespondAsync(text: $"Volume not implemented yet...", ephemeral: true);
        }

        private async Task CheckAndJoinVoice(SocketSlashCommand command)
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
                    // Attempt to connect to the voice channel
                    _audioClient = await voiceChannel.ConnectAsync();
                }
                catch (Exception ex)
                {
                    // Handle the exception if the connection fails
                    await command.RespondAsync($"Failed to connect to the voice channel: {ex.Message}", ephemeral: true);
                    return;
                }
            }
        }

        private void Log(string text)
        {
            Console.WriteLine($"[AudioManager] {text}");
        }
    }
}
