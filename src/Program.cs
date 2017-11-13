namespace BrockBot
{
    using System;
    using System.Threading.Tasks;

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
            var bot = new FilterBot();
            await bot.Start();

            Console.Read();
            await bot.Stop();
        }
    }
}