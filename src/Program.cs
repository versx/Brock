namespace BrockBot
{
    using System;
    using System.Threading.Tasks;

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
                Logger = new EventLogger(Log)
            };
            await bot.Start();

            Console.Read();
            await bot.Stop();
        }

        static void Log(LogType logType, string message)
        {
            Console.WriteLine($"{logType.ToString().ToUpper()} >> {message}");
            System.IO.File.AppendAllText(DateTime.Now.ToString("yyyy-MM-dd") + ".log", $"{logType.ToString().ToUpper()} >> {message}");
        }
    }
}