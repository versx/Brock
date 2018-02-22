namespace BrockBot
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BrockBot.Commands;
    using BrockBot.Diagnostics;

    class MainClass
    {
        const string Libs = "Libs";

        static string LogsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

        static FilterBot bot;

        static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine($"MainAsync [Arguments={string.Join(", ", args)}]");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            try
            {
                if (!Directory.Exists(LogsFolder))
                {
                    Directory.CreateDirectory(LogsFolder);
                }

                bot = new FilterBot(new EventLogger(OnLog));

                //General Commands
                bot.RegisterCommand<HelpCommand>();
                bot.RegisterCommand<TeamsCommand>();
                bot.RegisterCommand<TeamCommand>();
                bot.RegisterCommand<FeedsCommand>();
                bot.RegisterCommand<FeedMeCommand>();
                bot.RegisterCommand<FeedMeNotCommand>();
                bot.RegisterCommand<PokemonLookupCommand>();
                bot.RegisterCommand<MapCommand>();
                bot.RegisterCommand<DonateCommand>();
                bot.RegisterCommand<ScanListCommand>();
                bot.RegisterCommand<NearbyNestsCommand>();
                bot.RegisterCommand<GoogleCommand>();
                bot.RegisterCommand<DoorCommand>();
                bot.RegisterCommand<CheckApiCommand>();
                bot.RegisterCommand<GetWeatherCommand>();
                bot.RegisterCommand<VersionCommand>();

                bot.RegisterCommand<GiveawayCommand>();

                //Voting Commands
                bot.RegisterCommand<CreateVoteCommand>();

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
                //bot.RegisterCommand<CreateRaidLobbyCommand>();
                //bot.RegisterCommand<RaidLobbyCheckInCommand>();
                //bot.RegisterCommand<RaidLobbyOnTheWayCommand>();
                //bot.RegisterCommand<RaidLobbyCancelCommand>();
                //bot.RegisterCommand<RaidLobbyListUsersCommand>();

                //Reminder Commands
                bot.RegisterCommand<GetRemindersCommand>();
                bot.RegisterCommand<SetReminderCommand>();
                bot.RegisterCommand<DeleteReminderCommand>();

                //Twitter Commands
                bot.RegisterCommand<ListTwitterCommand>();
                bot.RegisterCommand<AddTwitterCommand>();
                bot.RegisterCommand<DeleteTwitterCommand>();
                bot.RegisterCommand<EnableDisableTwitterCommand>();

                //Moderator Commands
                bot.RegisterCommand<AssignEliteCommand>();
                bot.RegisterCommand<SetEncounterListCommand>();

                //Administrative Commands
                bot.RegisterCommand<CreateRolesCommand>();
                bot.RegisterCommand<DeleteRolesCommand>();
                bot.RegisterCommand<AssignRoleCommand>();
                bot.RegisterCommand<AssignRolesCommand>();
                bot.RegisterCommand<UptimeCommand>();
                bot.RegisterCommand<SetCommand>();
                bot.RegisterCommand<SayCommand>();
                //bot.RegisterCommand<LeaveGuildCommand>();
                bot.RegisterCommand<SwitchAccountsCommand>();
                bot.RegisterCommand<ReloadConfigCommand>();
                bot.RegisterCommand<RestartCommand>();
                bot.RegisterCommand<ShutdownCommand>();

                await bot.StartAsync();
                await Task.Delay(-1);
                await bot.StopAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        static void OnLog(LogType logType, string message)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: {logType.ToString().ToUpper()} >> {message}");
                File.AppendAllText(Path.Combine(LogsFolder, DateTime.Now.ToString("yyyy-MM-dd") + $"_{logType}.log"), $"{DateTime.Now.ToLongTimeString()}: {logType.ToString().ToUpper()} >> {message}\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }
        }

        static async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            OnLog(LogType.Error, $"IsTerminating: {e.IsTerminating}\r\n{((Exception)e.ExceptionObject).ToString()}");

            await bot.AlertOwnerOfCrash();

            if (e.IsTerminating)
            {
                Application.Restart();
            }
        }

        static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                if (args.Name.Contains("Autofac")) return GetAssembly("Autofac");
                else if (args.Name.Contains("DSharpPlus")) return GetAssembly("DSharpPlus");
                else if (args.Name.Contains("Newtonsoft.Json")) return GetAssembly("Newtonsoft.Json");
                else if (args.Name.Contains("Nito.AsyncEx.Context")) return GetAssembly("Nito.AsyncEx.Context");
                else if (args.Name.Contains("Nito.AsyncEx.Tasks")) return GetAssembly("Nito.AsyncEx.Tasks");
                else if (args.Name.Contains("Nito.Disposables")) return GetAssembly("Nito.Disposables");
                else if (args.Name.Contains("System.AppContext")) return GetAssembly("System.AppContext");
                else if (args.Name.Contains("System.Collections.Concurrent")) return GetAssembly("System.Collections.Concurrent");
                else if (args.Name.Contains("System.Collections.Immutable")) return GetAssembly("System.Collections.Immutable");
                else if (args.Name.Contains("System.Collections.NonGeneric")) return GetAssembly("System.Collections.NonGeneric");
                else if (args.Name.Contains("System.Collections.Specialized")) return GetAssembly("System.Collections.Specialized");
                else if (args.Name.Contains("System.Collections")) return GetAssembly("System.Collections");
                else if (args.Name.Contains("System.ComponentModel.EventBasedAsync")) return GetAssembly("System.ComponentModel.EventBasedAsync");
                else if (args.Name.Contains("System.ComponentModel.Primitives")) return GetAssembly("System.ComponentModel.Primitives");
                else if (args.Name.Contains("System.ComponentModel.TypeConverter")) return GetAssembly("System.ComponentModel.TypeConverter");
                else if (args.Name.Contains("System.ComponentModel")) return GetAssembly("System.ComponentModel");
                else if (args.Name.Contains("System.Console")) return GetAssembly("System.Console");
                else if (args.Name.Contains("System.Data.Common")) return GetAssembly("System.Data.Common");
                else if (args.Name.Contains("System.Diagnostics.Contracts")) return GetAssembly("System.Diagnostics.Contracts");
                else if (args.Name.Contains("System.Diagnostics.Debug")) return GetAssembly("System.Diagnostics.Debug");
                else if (args.Name.Contains("System.Diagnostics.FileVersionInfo")) return GetAssembly("System.Diagnostics.FileVersionInfo");
                else if (args.Name.Contains("System.Diagnostics.Process")) return GetAssembly("System.Diagnostics.Process");
                else if (args.Name.Contains("System.Diagnostics.StackTrace")) return GetAssembly("System.Diagnostics.StackTrace");
                else if (args.Name.Contains("System.Diagnostics.TextWriterTraceListener")) return GetAssembly("System.Diagnostics.TextWriterTraceListener");
                else if (args.Name.Contains("System.Diagnostics.Tools")) return GetAssembly("System.Diagnostics.Tools");
                else if (args.Name.Contains("System.Diagnostics.TraceSource")) return GetAssembly("System.Diagnostics.TraceSource");
                else if (args.Name.Contains("System.Diagnostics.Tracing")) return GetAssembly("System.Diagnostics.Tracing");
                else if (args.Name.Contains("System.Drawing.Primitives")) return GetAssembly("System.Drawing.Primitives");
                else if (args.Name.Contains("System.Dynamic.Runtime")) return GetAssembly("System.Dynamic.Runtime");
                else if (args.Name.Contains("System.Globalization.Calendars")) return GetAssembly("System.Globalization.Calendars");
                else if (args.Name.Contains("System.Globalization")) return GetAssembly("System.Globalization");
                else if (args.Name.Contains("System.IO.Compression.ZipFile")) return GetAssembly("System.IO.Compression.ZipFile");
                else if (args.Name.Contains("System.IO.Compression")) return GetAssembly("System.IO.Compression");
                else if (args.Name.Contains("System.IO.FileSystem.DriveInfo")) return GetAssembly("System.IO.FileSystem.DriveInfo");
                else if (args.Name.Contains("System.IO.FileSystem.Primitives")) return GetAssembly("System.IO.FileSystem.Primitives");
                else if (args.Name.Contains("System.IO.FileSystem.Watcher")) return GetAssembly("System.IO.FileSystem.Watcher");
                else if (args.Name.Contains("System.IO.IsolatedStorage")) return GetAssembly("System.IO.IsolatedStorage");
                else if (args.Name.Contains("System.IO.MemoryMappedFiles")) return GetAssembly("System.IO.MemoryMappedFiles");
                else if (args.Name.Contains("System.IO.Pipes")) return GetAssembly("System.IO.Pipes");
                else if (args.Name.Contains("System.IO.UnmanangedMemoryStream")) return GetAssembly("System.IO.UnmanangedMemoryStream");
                else if (args.Name.Contains("System.IO")) return GetAssembly("System.IO");
                else if (args.Name.Contains("System.Linq.Expressions")) return GetAssembly("System.Linq.Expressions");
                else if (args.Name.Contains("System.Linq.Parallel")) return GetAssembly("System.Linq.Parallel");
                else if (args.Name.Contains("System.Linq.Queryable")) return GetAssembly("System.Linq.Queryable");
                else if (args.Name.Contains("System.Linq")) return GetAssembly("System.Linq");
                else if (args.Name.Contains("System.Net.Http")) return GetAssembly("System.Net.Http");
                else if (args.Name.Contains("System.Net.NameResolution")) return GetAssembly("System.Net.NameResolution");
                else if (args.Name.Contains("System.Net.NetworkInformation")) return GetAssembly("System.Net.NetworkInformation");
                else if (args.Name.Contains("System.Net.Ping")) return GetAssembly("System.Net.Ping");
                else if (args.Name.Contains("System.Net.Primitives")) return GetAssembly("System.Net.Primitives");
                else if (args.Name.Contains("System.Net.Requests")) return GetAssembly("System.Net.Requests");
                else if (args.Name.Contains("System.Net.Security")) return GetAssembly("System.Net.Security");
                else if (args.Name.Contains("System.Net.Sockets")) return GetAssembly("System.Net.Sockets");
                else if (args.Name.Contains("System.Net.WebHeaderCollection")) return GetAssembly("System.Net.WebHeaderCollection");
                else if (args.Name.Contains("System.Net.WebSockets.Client")) return GetAssembly("System.Net.WebSockets.Client");
                else if (args.Name.Contains("System.Net.WebSockets")) return GetAssembly("System.Net.WebSockets");
                else if (args.Name.Contains("System.ObjectModel")) return GetAssembly("System.ObjectModel");
                else if (args.Name.Contains("System.Reflection.Extensions")) return GetAssembly("System.Reflection.Extensions");
                else if (args.Name.Contains("System.Reflection.Primitives")) return GetAssembly("System.Reflection.Primitives");
                else if (args.Name.Contains("System.Reflection.TypeExtensions")) return GetAssembly("System.Reflection.TypeExtensions");
                else if (args.Name.Contains("System.Reflection")) return GetAssembly("System.Reflection");
                else if (args.Name.Contains("System.Resources.Reader")) return GetAssembly("System.Resources.Reader");
                else if (args.Name.Contains("System.Resources.ResourceManager")) return GetAssembly("System.Resources.ResourceManager");
                else if (args.Name.Contains("System.Resources.Writer")) return GetAssembly("System.Resources.Writer");
                else if (args.Name.Contains("System.Runtime.CompilerServices.VisualC")) return GetAssembly("System.Runtime.CompilerServices.VisualC");
                else if (args.Name.Contains("System.Runtime.Extensions")) return GetAssembly("System.Runtime.Extensions");
                else if (args.Name.Contains("System.Runtime.Handles")) return GetAssembly("System.Runtime.Handles");
                else if (args.Name.Contains("System.Runtime.InteropServices.RuntimeInformation")) return GetAssembly("System.Runtime.InteropServices.RuntimeInformation");
                else if (args.Name.Contains("System.Runtime.InteropServices")) return GetAssembly("System.Runtime.InteropServices");
                else if (args.Name.Contains("System.Runtime.Numerics")) return GetAssembly("System.Runtime.Numerics");
                else if (args.Name.Contains("System.Runtime.Serialization.Formatters")) return GetAssembly("System.Runtime.Serialization.Formatters");
                else if (args.Name.Contains("System.Runtime.Serialization.Json")) return GetAssembly("System.Runtime.Serialization.Json");
                else if (args.Name.Contains("System.Runtime.Serialization.Primitives")) return GetAssembly("System.Runtime.Serialization.Primitives");
                else if (args.Name.Contains("System.Runtime.Serialization.Xml")) return GetAssembly("System.Runtime.Serialization.Xml");
                else if (args.Name.Contains("System.Runtime")) return GetAssembly("System.Runtime");
                else if (args.Name.Contains("System.Security.Claims")) return GetAssembly("System.Security.Claims");
                else if (args.Name.Contains("System.Security.Cryptography.Algorithms.")) return GetAssembly("System.Security.Cryptography.Algorithms");
                else if (args.Name.Contains("System.Security.Cryptography.Csp")) return GetAssembly("System.Security.Cryptography.Csp");
                else if (args.Name.Contains("System.Security.Cryptography.Encoding")) return GetAssembly("System.Security.Cryptography.Encoding");
                else if (args.Name.Contains("System.Security.Cryptography.Primitives")) return GetAssembly("System.Security.Cryptography.Primitives");
                else if (args.Name.Contains("System.Security.Cryptography.X509Certificates")) return GetAssembly("System.Security.Cryptography.X509Certificates");
                else if (args.Name.Contains("System.Security.Principal")) return GetAssembly("System.Security.Principal");
                else if (args.Name.Contains("System.Security.SecureString")) return GetAssembly("System.Security.SecureString");
                else if (args.Name.Contains("System.Text.Encoding.Extensions")) return GetAssembly("System.Text.Encoding.Extensions");
                else if (args.Name.Contains("System.Text.Encoding")) return GetAssembly("System.Text.Encoding");
                else if (args.Name.Contains("System.Text.RegularExpressions")) return GetAssembly("System.Text.RegularExpressions");
                else if (args.Name.Contains("System.Threading.Overlapped")) return GetAssembly("System.Threading.Overlapped");
                else if (args.Name.Contains("System.Threading.Tasks.Parallel")) return GetAssembly("System.Threading.Parallel");
                else if (args.Name.Contains("System.Threading.Tasks")) return GetAssembly("System.Threading.Tasks");
                else if (args.Name.Contains("System.Threading.Thread")) return GetAssembly("System.Threading.Thread");
                else if (args.Name.Contains("System.Threading.ThreadPool")) return GetAssembly("System.Threading.ThreadPool");
                else if (args.Name.Contains("System.Threading.Timer")) return GetAssembly("System.Threading.Timer");
                else if (args.Name.Contains("System.Threading")) return GetAssembly("System.Threading");
                else if (args.Name.Contains("System.ValueTuple")) return GetAssembly("System.ValueTuple");
                else if (args.Name.Contains("System.Xml.ReaderWriter")) return GetAssembly("System.Xml.ReaderWriter");
                else if (args.Name.Contains("System.Xml.XDocument")) return GetAssembly("System.Xml.XDocument");
                else if (args.Name.Contains("System.Xml.XmlDocument")) return GetAssembly("System.Xml.XmlDocument");
                else if (args.Name.Contains("System.Xml.Serializer")) return GetAssembly("System.Xml.Serializer");
                else if (args.Name.Contains("System.Xml.XPath.XDocument")) return GetAssembly("System.Xml.XPath.XDocument");
                else if (args.Name.Contains("System.Xml.XPath")) return GetAssembly("System.Xml.XPath");
                else if (args.Name.Contains("Tweetinvi.Controllers")) return GetAssembly("Tweetinvi.Controllers");
                else if (args.Name.Contains("Tweetinvi.Core")) return GetAssembly("Tweetinvi.Core");
                else if (args.Name.Contains("Tweetinvi.Credentials")) return GetAssembly("Tweetinvi.Credentials");
                else if (args.Name.Contains("Tweetinvi.Factories")) return GetAssembly("Tweetinvi.Factories");
                else if (args.Name.Contains("Tweetinvi.Logic")) return GetAssembly("Tweetinvi.Logic");
                else if (args.Name.Contains("Tweetinvi.Security")) return GetAssembly("Tweetinvi.Security");
                else if (args.Name.Contains("Tweetinvi.Streams")) return GetAssembly("Tweetinvi.Streams");
                else if (args.Name.Contains("Tweetinvi.WebLogic")) return GetAssembly("Tweetinvi.WebLogic");
                else if (args.Name.Contains("Tweetinvi")) return GetAssembly("Tweetinvi");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: AssemblyResolve: {ex}");
            }

            return null;
        }

        static Assembly GetAssembly(string name)
        {
            try
            {
                var path = Path.Combine(Path.Combine(Application.StartupPath, Libs), $"{name}.dll");
                var data = File.ReadAllBytes(path);
                return Assembly.Load(data);
            }
            catch
            {
                return null;
            }
        }
    }
}