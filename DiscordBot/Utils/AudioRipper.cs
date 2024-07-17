using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Utils
{
    internal static class AudioRipper
    {
        private static Queue<SongInfo> _songQueue = new();
        private static float GlobalVolume = .1f; // Global volume (1.0 = 100%)
        private static SemaphoreSlim _ffmpegSemaphore = new SemaphoreSlim(1, 1);
        private static SemaphoreSlim _ytDlpSemaphore = new SemaphoreSlim(1, 1);
        public static SemaphoreSlim PlaybackSemaphore = new SemaphoreSlim(1, 1);

        public static List<SongInfo> GetSongQueue()
        {
            return _songQueue.ToList();
        }

        public static async Task EnqueueYoutubeTask(IAudioClient client, string videoUrl, string title, CancellationToken cancellationToken)
        {
            var songInfo = new SongInfo(title, videoUrl);
            _songQueue.Enqueue(songInfo);

            // Start playing the song immediately if no song is currently playing
            if (_songQueue.Count == 1)
            {
                await PlayNextSong(client, cancellationToken);
            }
        }


        public static async Task PlayNextSong(IAudioClient client, CancellationToken cancellationToken)
        {
            if (_songQueue.Count == 0)
                return;

            await PlaybackSemaphore.WaitAsync(cancellationToken); // Wait to ensure only one playback at a time

            var nextSong = _songQueue.Peek();
            try
            {
                // Update status when the bot is ready
                await BotManager.Instance.Client.SetGameAsync($"Playing {nextSong.Title}");
                await SendAsync(client, nextSong.Url, cancellationToken);

                // Remove the song from the queue after it finishes playing
                _songQueue.Dequeue();

                // Play the next song in the queue
                if (_songQueue.Count > 0)
                {
                    await PlayNextSong(client, cancellationToken);
                }
                else
                {
                    await BotManager.Instance.Client.SetGameAsync($"Dominating the world...");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Playback was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing song: {ex.Message}");
                // Log the exception here if needed
            }
            finally
            {
                PlaybackSemaphore.Release(); // Release the semaphore
            }
        }


        public static async Task SendAsync(IAudioClient client, string url, CancellationToken cancellationToken)
        {
            try
            {
                if (IsYouTubeUrl(url))
                {
                    await SendAsyncYoutube(client, url, cancellationToken);
                }
                else
                {
                    await SendAsyncDirect(client, url, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Audio Ripper] Audio streaming was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Audio Ripper]Error during audio streaming: {ex.Message}");
                // Log the exception here if needed
            }
        }

        private static async Task SendAsyncDirect(IAudioClient client, string url, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _ffmpegSemaphore.WaitAsync();
                var ffmpeg = CreateStream(url);
                var output = ffmpeg.StandardOutput.BaseStream;
                var discord = client.CreatePCMStream(AudioApplication.Mixed);

                try
                {
                    await output.CopyToAsync(discord, cancellationToken);
                    await discord.FlushAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Handle the exception (log it, notify the user, etc.)
                    Console.WriteLine($"Error occurred: {ex.Message}");
                }
                finally
                {
                    await discord.DisposeAsync();
                    ffmpeg.Dispose();
                    _ffmpegSemaphore.Release();
                }

                // Optionally, add a delay before retrying to avoid spamming
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }


        private static async Task SendAsyncYoutube(IAudioClient client, string videoUrl, CancellationToken cancellationToken)
        {
            await _ytDlpSemaphore.WaitAsync();

            var ytDlpProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"-f bestaudio -g {videoUrl}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            ytDlpProcess.Start();
            string streamUrl = await ytDlpProcess.StandardOutput.ReadLineAsync();
            await ytDlpProcess.WaitForExitAsync();

            if (ytDlpProcess.ExitCode != 0)
            {
                string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"yt-dlp error: {error}");
                // Log the error here if needed
                _ytDlpSemaphore.Release();
                return;
            }

            _ytDlpSemaphore.Release();
            await SendAsyncDirect(client, streamUrl, cancellationToken);
        }

        private static Process CreateStream(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel error -i \"{url}\" -af \"volume={GlobalVolume}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            Task.Run(() =>
            {
                try
                {
                    string line;
                    while ((line = process.StandardError.ReadLine()) != null)
                    {
                        Console.WriteLine($"ffmpeg error: {line}");
                        // Log ffmpeg errors here if needed
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading ffmpeg output: {ex.Message}");
                    // Log the exception here if needed
                }
            });

            return process;
        }

        public static async Task<string> GetSongTitle(string videoUrl)
        {
            await _ytDlpSemaphore.WaitAsync();

            var ytDlpProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--get-title {videoUrl}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            ytDlpProcess.Start();
            string title = await ytDlpProcess.StandardOutput.ReadLineAsync();
            await ytDlpProcess.WaitForExitAsync();

            if (ytDlpProcess.ExitCode != 0)
            {
                string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"yt-dlp error: {error}");
                // Log the error here if needed
                _ytDlpSemaphore.Release();
                return null;
            }

            _ytDlpSemaphore.Release();
            return title;
        }

        private static bool IsYouTubeUrl(string url)
        {
            return url.Contains("youtube.com") || url.Contains("youtu.be");
        }
    }

    public class SongInfo
    {
        public string Title { get; }
        public string Url { get; }

        public SongInfo(string title, string url)
        {
            Title = title;
            Url = url;
        }
    }
}
