using Discord.WebSocket;
using Discord;
using DiscordBot.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.AudioBot.Commands
{
    internal class CommandQueue : IDiscordCommand
    {
        private string _commandName = "queue";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Displays current queue");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            _ = Task.Run(async () => {
                await AudioManager.Instance.SongQueue(command);
            });
        }

    }
}
