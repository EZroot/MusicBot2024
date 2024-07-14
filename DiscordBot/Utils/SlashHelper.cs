using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Utils
{
    public static class SlashHelper
    {
        private static async Task SlashCommandAdd(SocketGuild guild, SlashCommandBuilder[] commands)
        {
            // Clear existing commands
            _ = Task.Run(async () =>
            {
                foreach (var command in commands)
                {
                    await guild.CreateApplicationCommandAsync(command.Build());
                }
            });
        }
        private static async Task SlashCommandClear(SocketGuild guild)
        {
            // Clear existing commands
            _ = Task.Run(async () =>
            {
                var commands = await guild.GetApplicationCommandsAsync();
                foreach (var command in commands)
                {
                    await command.DeleteAsync();
                }
            });
        }
    }
}
