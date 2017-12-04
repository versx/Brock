namespace BrockBot
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using BrockBot.Commands;
    using BrockBot.Diagnostics;

    /** Usage
     * .subs - List all current subscriptions.
     * .sub <pokemon_name> - Subscribes to notifications of messages containing the specified keyword.
     * .unsub - Unsubscribe from all notifications.
     * .unsub <pokemon_name> - Unsubscribes from notifications of messages containing the specified keyword.
     * 
     */

    class MainClass
    {
        public static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            var logsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            if (!Directory.Exists(logsFolder))
            {
                Directory.CreateDirectory(logsFolder);
            }

            var bot = new FilterBot
            {
                Logger = new EventLogger((logType, message)=>
                {
                    Console.WriteLine($"{logType.ToString().ToUpper()} >> {message}");
                    File.AppendAllText(Path.Combine(logsFolder, DateTime.Now.ToString("yyyy-MM-dd") + ".log"), $"{logType.ToString().ToUpper()} >> {message}");
                })
            };
            //General Commands
            bot.RegisterCommand<HelpCommand>();
            bot.RegisterCommand<TeamCommand>();
            bot.RegisterCommand<DemoCommand>();
            bot.RegisterCommand<VersionCommand>();
            bot.RegisterCommand<PokemonLookupCommand>();
            //Information Commands
            bot.RegisterCommand<MapCommand>();
            bot.RegisterCommand<DonateCommand>();
            //Notification Commands
            bot.RegisterCommand<InfoCommand>();
            bot.RegisterCommand<AddCommand>();
            bot.RegisterCommand<RemoveCommand>();
            bot.RegisterCommand<SubscribeCommand>();
            bot.RegisterCommand<UnsubscribeCommand>();
            bot.RegisterCommand<EnableDisableCommand>(true);
            bot.RegisterCommand<EnableDisableCommand>(false);
            //Raid Lobby Commands
            bot.RegisterCommand<CreateRaidLobbyCommand>();
            bot.RegisterCommand<RaidLobbyCheckInCommand>();
            bot.RegisterCommand<RaidLobbyOnTheWayCommand>();
            bot.RegisterCommand<RaidLobbyCancelCommand>();
            bot.RegisterCommand<RaidLobbyListUsersCommand>();
            //Administrative Commands
            bot.RegisterCommand<CreateRolesCommand>();
            bot.RegisterCommand<DeleteRolesCommand>();
            bot.RegisterCommand<UptimeCommand>();
            bot.RegisterCommand<SetCommand>();
            bot.RegisterCommand<RestartCommand>();
            bot.RegisterCommand<ShutdownCommand>();
            await bot.StartAsync();

            Console.Read();
            await bot.StartAsync();
        }
    }
}