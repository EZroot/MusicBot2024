using Discord;
using Discord.Audio;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot;
using DiscordBot.AudioBot;
using DiscordBot.Commands;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main()
    {
        await BotManager.Instance.Initialize();
    }
}
