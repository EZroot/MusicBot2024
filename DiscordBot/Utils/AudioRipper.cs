using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static async Task EnqueueYoutubeTask(IAudioClient client, string videoUrl, string title)
        {
            _songQueue.Enqueue(new SongInfo(title, videoUrl));
            await _taskQueue.Enqueue(() => SendAsyncYoutube(client, videoUrl));
        }

        private static async Task SendAsync(IAudioClient client, string path)
        {
            var ffmpeg = CreateStream(path);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Mixed);

            try
            {
                await output.CopyToAsync(discord);
                await discord.FlushAsync();
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

        private static async Task SendAsyncYoutube(IAudioClient client, string videoUrl)
        {
            // Generate a unique filename using GUID
            string fileName = $"{Guid.NewGuid()}.mp4";

            // Start yt-dlp process to download the audio
            var ytDlpProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"-f bestaudio --no-playlist -o \"{fileName}\" {videoUrl}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            ytDlpProcess.Start();
            await ytDlpProcess.WaitForExitAsync(); // Wait for yt-dlp to finish

            if (ytDlpProcess.ExitCode != 0)
            {
                string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"yt-dlp error: {error}");
                return;
            }

            // Use the downloaded file with ffmpeg
            await SendAsync(client, fileName);
            // Remove the song from the queue after playing
            if (_songQueue.Count > 0)
            {
                _songQueue.Dequeue();
            }
        }


        private static Process CreateStream(string path)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel error -i \"{path}\" -af \"volume={GlobalVolume}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true // Redirect standard error to capture ffmpeg errors
                }
            };

            process.Start();

            // Log any errors from ffmpeg
            Task.Run(() =>
            {
                string line;
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    Console.WriteLine($"ffmpeg error: {line}");
                }
            });

            return process;
        }

        public static async Task<string> GetSongTitle(string videoUrl)
        {
            // Start yt-dlp process to get the title
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
            await ytDlpProcess.WaitForExitAsync(); // Wait for yt-dlp to finish

            if (ytDlpProcess.ExitCode != 0)
            {
                string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"yt-dlp error: {error}");
                return null;
            }

            return title; // Return the title of the song
        }
    }
    public class SongInfo
    {
        public string Title;
        public string Url;
        public SongInfo(string title, string url)
        {
            this.Title = title;
            this.Url = url;
        }
    }
}


