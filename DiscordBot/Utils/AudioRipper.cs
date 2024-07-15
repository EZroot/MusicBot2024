using Discord;
using Discord.Audio;
using System.Diagnostics;

namespace DiscordBot.Utils
{
    internal static class AudioRipper
    {
        private static TaskQueue _taskQueue = new();
        private static Queue<SongInfo> _songQueue = new();
        private static float GlobalVolume = 1.0f; // Global volume (1.0 = 100%)

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

            var nextSong = _songQueue.Peek();
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

        public static async Task SendAsync(IAudioClient client, string url, CancellationToken cancellationToken)
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

        private static async Task SendAsyncDirect(IAudioClient client, string url, CancellationToken cancellationToken)
        {
            var ffmpeg = CreateStream(url);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Mixed);

            try
            {
                await output.CopyToAsync(discord, cancellationToken);
                await discord.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Audio streaming was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during audio streaming: {ex}");
            }
            finally
            {
                await discord.DisposeAsync();
                ffmpeg.Dispose();
            }
        }

        private static async Task SendAsyncYoutube(IAudioClient client, string videoUrl, CancellationToken cancellationToken)
        {
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
                return;
            }

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
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading ffmpeg output: {ex.Message}");
                }
            });

            return process;
        }

        public static async Task<string> GetSongTitle(string videoUrl)
        {
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
                return null;
            }

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
