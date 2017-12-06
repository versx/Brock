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
            Console.WriteLine($"Arguments: {string.Join(", ", args)}");

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
                    File.AppendAllText(Path.Combine(logsFolder, DateTime.Now.ToString("yyyy-MM-dd") + ".log"), $"{logType.ToString().ToUpper()} >> {message}\r\n");
                })
            };
            //General Commands
            bot.RegisterCommand<HelpCommand>();
            bot.RegisterCommand<TeamCommand>();
            bot.RegisterCommand<InviteCommand>();
            bot.RegisterCommand<InvitesCommand>();
            bot.RegisterCommand<PokemonLookupCommand>();
            bot.RegisterCommand<VersionCommand>();
            bot.RegisterCommand<BansCommand>();

            //Information Commands
            bot.RegisterCommand<MapCommand>();
            bot.RegisterCommand<DonateCommand>();

            //Custom Commands
            bot.RegisterCommand<ListCmdCommand>();
            bot.RegisterCommand<AddCmdCommand>();
            bot.RegisterCommand<EditCmdCommand>();
            bot.RegisterCommand<DelCmdCommand>();

            //Notification Commands
            bot.RegisterCommand<DemoCommand>();
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
            bot.RegisterCommand<SayCommand>();
            bot.RegisterCommand<LeavelGuildCommand>();
            bot.RegisterCommand<BanCommand>();
            bot.RegisterCommand<UnbanCommand>();
            bot.RegisterCommand<RestartCommand>();
            bot.RegisterCommand<ShutdownCommand>();

            //TODO: Auto-post delete after x minutes/hours etc :arrows_counterclockwise: 
            await bot.StartAsync();

            Console.Read();
            await bot.StopAsync();
        }
    }
}