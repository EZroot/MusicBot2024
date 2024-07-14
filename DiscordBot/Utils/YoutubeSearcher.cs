using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public static class YoutubeSearcher
{
    public static List<YoutubeResult> YoutubeResults { get; private set; }
    public static bool ResultsReady { get; set; }
    public static async Task<List<YoutubeResult>> SearchYoutube(string query, int maxResults = 4)
    {
        var results = new List<YoutubeResult>();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"ytsearch{maxResults}:\"{query}\" --flat-playlist --print \"title,url\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            Console.WriteLine($"Starting yt-dlp process with query: {query}");
            process.Start();

            Console.WriteLine("Reading standard output...");
            string output = await process.StandardOutput.ReadToEndAsync();
            Console.WriteLine("Reading standard error...");
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();
            Console.WriteLine($"yt-dlp process exited with code {process.ExitCode}");

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"yt-dlp error: {error}");
                return results;
            }

            Console.WriteLine("Parsing output...");
            string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i += 2)
            {
                if (i + 1 < lines.Length)
                {
                    var title = lines[i].Trim();
                    var url = lines[i + 1].Trim();

                    Console.WriteLine($"Found result: Title = {title} URL = {url}");
                    results.Add(new YoutubeResult
                    {
                        Title = title,
                        Url = url
                    });
                }
            }

            YoutubeResults = results;
            ResultsReady = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
        }
        return results;
    }

    public class YoutubeResult
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
