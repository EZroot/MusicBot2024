using Discord;
using Discord.WebSocket;
using System.Reflection;

namespace DiscordBot.Commands
{
    internal class CommandManager
    {
        // The static instance of the singleton
        private static CommandManager _instance;

        // Lock object for thread safety
        private static readonly object _lock = new();

        // Private constructor to prevent instantiation
        private CommandManager()
        {
            // Initialize any necessary resources here
        }

        // Public method to access the singleton instance
        public static CommandManager Instance
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
                            _instance = new CommandManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, IDiscordCommand> _commands = new();
        private readonly List<SlashCommandBuilder> _slashCommandBuilder = new();

        public async Task RegisterAllCommands(SocketGuild guild)
        {
            // Get all types that implement IDiscordCommand
            var commandTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IDiscordCommand).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var commandType in commandTypes)
            {
                // Create an instance of the command
                var commandInstance = (IDiscordCommand)Activator.CreateInstance(commandType);
                // Register the command
                RegisterCommand(commandInstance);
            }

            Log("Building commands...");
            await BuildAllCommands(guild);
        }

        public async Task ExecuteCommand(SocketSlashCommand slashCommand)
        {
            if (_commands.TryGetValue(slashCommand.Data.Name, out var command))
            {
                Log($"{slashCommand.User.Username}:> {slashCommand.Data.Name}");
                await command.ExecuteAsync(slashCommand);
            }
            else
            {
                Console.WriteLine("Command not found.");
            }
        }
        private async Task BuildAllCommands(SocketGuild guild)
        {
            // Clear existing commands
            _ = Task.Run(async () =>
            {
                foreach (var command in _slashCommandBuilder)
                {
                    await guild.CreateApplicationCommandAsync(command.Build());
                }
            });
        }

        private void RegisterCommand(IDiscordCommand command)
        {
            Log($"Registering {command.CommandName}");
            _slashCommandBuilder.Add(command.Register());
            AddCommand(command.CommandName, command);
        }

        private void AddCommand(string name, IDiscordCommand command)
        {
            _commands[name] = command;
        }

        private void Log(string text)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss"); // Format to include only hour, minute, and second
            Console.ForegroundColor = ConsoleColor.Yellow; // Purple color
            Console.Write($"{timeStamp} [CommandManager] ");
            Console.ForegroundColor = ConsoleColor.White; // Green color
            Console.WriteLine(text);
            Console.ResetColor(); // Reset to default color
        }
    }
}
