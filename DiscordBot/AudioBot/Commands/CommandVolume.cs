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
    internal class CommandVolume : IDiscordCommand
    {
        private string _commandName = "volume";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Joins a voice channel and plays audio")
            .AddOption("vol", ApplicationCommandOptionType.Number, "volume", isRequired: true);
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            _ = Task.Run(async () => {
                await AudioManager.Instance.ChangeVolume(command);
            });
        }

    }
}
