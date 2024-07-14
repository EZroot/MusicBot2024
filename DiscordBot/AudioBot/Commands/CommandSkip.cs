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
    internal class CommandSkip : IDiscordCommand
    {
        private string _commandName = "skip";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Skips the current song");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            _ = Task.Run(async () => {
                await AudioManager.Instance.SkipSong(command);
            });
        }

    }
}
