using Discord;
using Discord.WebSocket;
using DiscordBot.AudioBot;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    internal class CommandPlay : IDiscordCommand
    {
        private string _commandName = "play";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Joins a voice channel and plays audio")
            .AddOption("url", ApplicationCommandOptionType.String, "Youtube url", isRequired: true);
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            var arg = command.Data.Options.First().Value;

            _ = Task.Run(async () => {
                await AudioManager.Instance.PlaySong(command);
            });
        }

    }
}
