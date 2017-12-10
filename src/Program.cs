namespace BrockBot
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BrockBot.Commands;
    using BrockBot.Diagnostics;

    class MainClass
    {
        static string LogsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

        static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Console.WriteLine($"Arguments: {string.Join(", ", args)}");

            if (!Directory.Exists(LogsFolder))
            {
                Directory.CreateDirectory(LogsFolder);
            }

            var bot = new FilterBot { Logger = new EventLogger(Log) };

            //General Commands
            bot.RegisterCommand<HelpCommand>();
            bot.RegisterCommand<TeamCommand>();
            bot.RegisterCommand<TeamsCommand>();
            bot.RegisterCommand<CitiesCommand>();
            bot.RegisterCommand<FeedCommand>();
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

            //Twitter Commands
            bot.RegisterCommand<ListTwitterCommand>();
            bot.RegisterCommand<AddTwitterCommand>();
            bot.RegisterCommand<DeleteTwitterCommand>();
            bot.RegisterCommand<EnableDisableTwitterCommand>();

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

        static void Log(LogType logType, string message)
        {
            Console.WriteLine($"{logType.ToString().ToUpper()} >> {message}");
            File.AppendAllText(Path.Combine(LogsFolder, DateTime.Now.ToString("yyyy-MM-dd") + ".log"), $"{logType.ToString().ToUpper()} >> {message}\r\n");
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log(LogType.Error, $"IsTerminating: {e.IsTerminating}\r\n{((Exception)e.ExceptionObject).ToString()}");

            if (e.IsTerminating)
            {
                Application.Restart();
            }
        }
    }
}