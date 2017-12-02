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
            var bot = new FilterBot
            {
                Logger = new EventLogger((logType, message)=>
                {
                    Console.WriteLine($"{logType.ToString().ToUpper()} >> {message}");
                    File.AppendAllText(DateTime.Now.ToString("yyyy-MM-dd") + ".log", $"{logType.ToString().ToUpper()} >> {message}");
                })
            };
            bot.RegisterCommand<DemoCommand>();
            bot.RegisterCommand<HelpCommand>();
            bot.RegisterCommand<InfoCommand>();
            bot.RegisterCommand<VersionCommand>();
            bot.RegisterCommand<AddCommand>();
            bot.RegisterCommand<AddCommand>();
            bot.RegisterCommand<RemoveCommand>();
            bot.RegisterCommand<SubscribeCommand>();
            bot.RegisterCommand<UnsubscribeCommand>();
            bot.RegisterCommand<EnableDisableCommand>(true);
            bot.RegisterCommand<EnableDisableCommand>(false);
            bot.RegisterCommand<TeamCommand>();
            bot.RegisterCommand<CreateRolesCommand>();
            bot.RegisterCommand<DeleteRolesCommand>();
            bot.RegisterCommand<CreateRaidLobbyCommand>();
            bot.RegisterCommand<RaidLobbyCheckInCommand>();
            bot.RegisterCommand<RaidLobbyOnTheWayCommand>();
            bot.RegisterCommand<RaidLobbyCancelCommand>();
            bot.RegisterCommand<RaidLobbyListUsersCommand>();
            bot.RegisterCommand<RestartCommand>();
            bot.RegisterCommand<ShutdownCommand>();
            bot.RegisterCommand<PokemonLookupCommand>();
            bot.RegisterCommand<MapCommand>();
            bot.RegisterCommand<UptimeCommand>();
            bot.RegisterCommand<DonateCommand>();
            bot.RegisterCommand<SetCommand>();
            await bot.StartAsync();

            Console.Read();
            await bot.StartAsync();
        }
    }
}