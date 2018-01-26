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

        static FilterBot bot;

        static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine($"MainAsync [Arguments={string.Join(", ", args)}]");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                if (!Directory.Exists(LogsFolder))
                {
                    Directory.CreateDirectory(LogsFolder);
                }

                bot = new FilterBot { Logger = new EventLogger(Log) };

                //General Commands
                bot.RegisterCommand<HelpCommand>();
                bot.RegisterCommand<TeamsCommand>();
                bot.RegisterCommand<TeamCommand>();
                bot.RegisterCommand<NearbyNestsCommand>();
                bot.RegisterCommand<FeedsCommand>();
                bot.RegisterCommand<FeedMeCommand>();
                bot.RegisterCommand<FeedMeNotCommand>();
                bot.RegisterCommand<PokemonLookupCommand>();
                bot.RegisterCommand<MapCommand>();
                bot.RegisterCommand<DonateCommand>();
                bot.RegisterCommand<ScanListCommand>();
                bot.RegisterCommand<CheckApiCommand>();
                bot.RegisterCommand<VersionCommand>();

                //Custom Commands
                bot.RegisterCommand<ListCmdCommand>();
                bot.RegisterCommand<AddCmdCommand>();
                bot.RegisterCommand<EditCmdCommand>();
                bot.RegisterCommand<DelCmdCommand>();

                //Notification Commands
                bot.RegisterCommand<DemoCommand>();
                bot.RegisterCommand<InfoCommand>();
                bot.RegisterCommand<PokeMeCommand>();
                bot.RegisterCommand<PokeMeNotCommand>();
                bot.RegisterCommand<RaidMeCommand>();
                bot.RegisterCommand<RaidMeNotCommand>();
                bot.RegisterCommand<EnableDisableCommand>();

                //Raid Lobby Commands
                bot.RegisterCommand<CreateRaidLobbyCommand>();
                bot.RegisterCommand<RaidLobbyCheckInCommand>();
                bot.RegisterCommand<RaidLobbyOnTheWayCommand>();
                bot.RegisterCommand<RaidLobbyCancelCommand>();
                bot.RegisterCommand<RaidLobbyListUsersCommand>();

                //Reminder Commands
                bot.RegisterCommand<GetRemindersCommand>();
                bot.RegisterCommand<SetReminderCommand>();
                bot.RegisterCommand<DeleteReminderCommand>();

                //Twitter Commands
                bot.RegisterCommand<ListTwitterCommand>();
                bot.RegisterCommand<AddTwitterCommand>();
                bot.RegisterCommand<DeleteTwitterCommand>();
                bot.RegisterCommand<EnableDisableTwitterCommand>();

                //Administrative Commands
                bot.RegisterCommand<CreateRolesCommand>();
                bot.RegisterCommand<DeleteRolesCommand>();
                bot.RegisterCommand<AssignRolesCommand>();
                bot.RegisterCommand<UptimeCommand>();
                //bot.RegisterCommand<SetCommand>();
                bot.RegisterCommand<SayCommand>();
                //bot.RegisterCommand<LeaveGuildCommand>();
                bot.RegisterCommand<SetEncounterListCommand>();
                bot.RegisterCommand<SwitchAccountsCommand>();
                bot.RegisterCommand<RestartCommand>();
                bot.RegisterCommand<ShutdownCommand>();

                await bot.StartAsync();

                Console.Read();
                await bot.StopAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        static void Log(LogType logType, string message)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: {logType.ToString().ToUpper()} >> {message}");
                File.AppendAllText(Path.Combine(LogsFolder, DateTime.Now.ToString("yyyy-MM-dd") + ".log"), $"{DateTime.Now.ToLongTimeString()}: {logType.ToString().ToUpper()} >> {message}\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }
        }

        static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log(LogType.Error, $"IsTerminating: {e.IsTerminating}\r\n{((Exception)e.ExceptionObject).ToString()}");

            await bot.AlertOwnerOfCrash();

            if (e.IsTerminating)
            {
                Application.Restart();
            }
        }
    }
}