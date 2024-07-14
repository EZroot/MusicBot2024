using Discord;
using Discord.WebSocket;
using DiscordBot.AudioBot;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    internal class CommandSearch : IDiscordCommand
    {
        private string _commandName = "search";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Searches youtube based on the keyword")
            .AddOption("key", ApplicationCommandOptionType.String, "Search...", isRequired: true);
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            var arg = (string)(command.Data.Options.First().Value);

            _ = Task.Run(async () => {

                await command.RespondAsync($"Searching: {arg}");
                await AudioManager.Instance.CheckAndJoinVoice(command);
                var result = await YoutubeSearcher.SearchYoutube(arg);
                if(result == null)
                {
                    await command.ModifyOriginalResponseAsync((m) => m.Content = "Error failed to search youtube.");
                    return;
                }
                var message = $"***SEARCHED:*** **{arg}**\n```";
                for (int i = 0; i < result.Count; i++)
                {
                    YoutubeSearcher.YoutubeResult? r = result[i];
                    message += $"#{i} {r.Title}\n";
                }
                message += "```";
                var messageList = await command.ModifyOriginalResponseAsync((m) => m.Content = message);
                var emojiIds = new ulong[] { 429753831199342592, 466478794367041557, 466477774455177247, 582418378178822144 };
                for (int i = 0; i < emojiIds.Length; i++)
                {
                    var emote = Emote.Parse($"<:warrior{i}:{emojiIds[i]}>");
                    await messageList.AddReactionAsync(emote);
                }
            });
        }

    }
}
