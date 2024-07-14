using Discord;
using Discord.Audio;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.AudioBot;
using DiscordBot.Commands;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class Program
{
    private static DiscordSocketClient _client;
    private static IAudioClient _audioClient;

    public static async Task Main()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildVoiceStates
        });

        _client.Log += Log;

        // Ensure your bot token is stored securely.
        var token = "Mzg3MjY5Nzc1MjY2NDE0NTky.G_y_DH.9LGrHOJrUGbDgr1hUbxvhDR5maoS4jBdiKX4R0";  // Replace with your actual bot token.

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Hook into the Ready event
        _client.Ready += Client_Ready;
        _client.ReactionAdded += ReactionAddedAsync;

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    private static async Task Client_Ready()
    {
        // Ensure you have the correct guild ID
        ulong guildId = 308708637679812608;  // Replace with your actual guild ID.
        var guild = _client.GetGuild(guildId);


        //SlashCommandClear(guild);
        await CommandManager.Instance.RegisterAllCommands(guild);
        _client.SlashCommandExecuted += SlashCommandHandler;
    }

    private static async Task SlashCommandHandler(SocketSlashCommand command)
    {
        await CommandManager.Instance.ExecuteCommand(command);
    }

    private static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cacheable, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        // Ensure the message is cached
        var message = await cacheable.GetOrDownloadAsync();
        if (message == null) return;
        if (!YoutubeSearcher.ResultsReady) return;
        // Check if the reaction is the one you're interested in
        // Replace with your custom emoji ID and name
        var emojiIds = new ulong[] { 429753831199342592, 466478794367041557, 466477774455177247, 582418378178822144 };
        for (int i = 0; i < emojiIds.Length; i++)
        {
            var emote = Emote.Parse($"<:warrior{i}:{emojiIds[i]}>");

            if (reaction.Emote is Emote e && e.Id == emojiIds[i])
            {
                var user = reaction.User.IsSpecified ? reaction.User.Value : null;
                if (user == null) return;
                if (user.IsBot) return;

                YoutubeSearcher.ResultsReady = false;

                Console.WriteLine($"{user.Username} added the reaction {emote.Name} to the message {message.Id}");
                _ = Task.Run(async () =>
                {
                    await AudioManager.Instance.PlaySong(YoutubeSearcher.YoutubeResults[i].Title, YoutubeSearcher.YoutubeResults[i].Url);
                });
                await message.ModifyAsync((m) => m.Content = $"Adding {YoutubeSearcher.YoutubeResults[i].Title} to Queue");
                // Handle the reaction added event as needed
                // Wait for the specified delay
                await Task.Delay(5 * 1000);

                // Delete the message
                await message.DeleteAsync();
            }
        }

       
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine($"[Program] {msg.ToString()}");
        return Task.CompletedTask;
    }

}
