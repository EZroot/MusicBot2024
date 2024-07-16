using Discord;
using Discord.WebSocket;
using DiscordBot.AudioBot;
using DiscordBot.Commands;
using System.Reflection;

namespace DiscordBot
{
    internal class BotManager
    {
        // The static instance of the singleton
        private static BotManager _instance;

        // Lock object for thread safety
        private static readonly object _lock = new();

        // Private constructor to prevent instantiation
        private BotManager()
        {
            // Initialize any necessary resources here
        }

        // Public method to access the singleton instance
        public static BotManager Instance
        {
            get
            {
                // Double-check locking for thread safety
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new BotManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private DiscordSocketClient _client;

        public DiscordSocketClient Client => _client;

        public async Task Initialize()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildVoiceStates
            });

            _client.Log += Log;

            var token = ConfigManager.LoadConfig();

            await _client.LoginAsync(TokenType.Bot, token.ApiKey);
            await _client.StartAsync();

            // Hook into the Ready event
            _client.Ready += Client_Ready;
            _client.ReactionAdded += ReactionAddedAsync;

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task Client_Ready()
        {
            // Ensure you have the correct guild ID
            ulong guildId = 308708637679812608;  // Replace with your actual guild ID.
            var guild = _client.GetGuild(guildId);


            //SlashCommandClear(guild);
            await CommandManager.Instance.RegisterAllCommands(guild);
            _client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            await CommandManager.Instance.ExecuteCommand(command);
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cacheable, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            // Ensure the message is cached
            var message = await cacheable.GetOrDownloadAsync();
            if (message == null) return;
            if (!YoutubeSearcher.ResultsReady) return;
            // Check if the reaction is the one you're interested in
            // Replace with your custom emoji ID and name
            _ = Task.Run(async () =>
            {
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
            });
        }

        private static Task Log(LogMessage msg)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss"); // Format to include only hour, minute, and second

            Console.ForegroundColor = ConsoleColor.Cyan; // Purple color
            Console.Write($"{timeStamp} [BotManager] ");
            Console.ForegroundColor = ConsoleColor.White; // Green color
            Console.WriteLine(msg.ToString());
            Console.ResetColor(); // Reset to default color

            return Task.CompletedTask;
        }
    }
}
