using Discord;
using Discord.Audio;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.Commands;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class Program
{
    private const string COMMAND_PLAY = "play";
    private const string COMMAND_QUEUE = "queue";
    private const string COMMAND_SKIP = "skip";
    private const string COMMAND_VOLUME = "volume";
    private const string COMMAND_LEAVE = "leave";


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

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine($"[Program] {msg.ToString()}");
        return Task.CompletedTask;
    }

}
