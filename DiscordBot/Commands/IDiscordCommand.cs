using Discord;
using Discord.WebSocket;
using System;

namespace DiscordBot.Commands
{
    internal interface IDiscordCommand
    {
        string CommandName { get; }
        SlashCommandBuilder Register();
        Task ExecuteAsync(SocketSlashCommand options);
    }
}
